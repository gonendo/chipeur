using System;
using System.Threading;
using static SDL2.SDL;
using chipeur.cpu;
using chipeur.graphics;
using chipeur.input;
using chipeur.sound;
using chipeur.gui;

namespace chipeur
{
    class Program
    {
        public const string VERSION = "2.1";
        private static bool _running = true;
        private static Chip8 _chip8;
        private static CancellationTokenSource _cts;
        private static bool _chip8EmulationTimer;
        private static bool _chip8TimersTimer;

        [STAThread]
        static void Main(string[] args)
        {
            if(SDL_Init(SDL_INIT_VIDEO | SDL_INIT_AUDIO) < 0){
                throw new Exception("Can't initialize SDL : "+SDL_GetError());
            }
            
            Gui gui = new Gui();
            Gui.Quit += () =>
            {
                _running = false;
            };
            Gui.ChangeSpeed += (int speedInHz) =>
            {
                Chip8.speedInHz = speedInHz;
                StartEmulationThread();
            };
            Gui.LoadRom += (string romPath) =>
            {
                _chip8.Initialize();
                _chip8.LoadGame(romPath);
                StartEmulationThread();
            };

            Graphics graphics = new Graphics();
            graphics.Initialize();

            Sounds.Initialize();

            Input input = new Input();
            input.Initialize();

            _chip8 = new Chip8(input);
            _chip8.Initialize();
            if(args.Length > 0){
                _chip8.LoadGame(args[0]);
            }
            else{
                Gui.menuBarVisible = true;
            }

            StartEmulationThread();

            _chip8TimersTimer = true;
            CancellationTokenSource cts2 = new CancellationTokenSource();
            ThreadPool.QueueUserWorkItem(new WaitCallback(DecreaseChip8Timers), cts2.Token);

            while(_running){
                if(_chip8.drawFlag){
                    _chip8.drawFlag = false;
                    graphics.Draw(_chip8.gfx);
                }

                if(_chip8.needToBeep){
                    Sounds.Beep();
                    _chip8.needToBeep = false;
                }

                gui.Update(graphics.pixelsBuffer);
            }

            StopEmulationThread();
            _chip8TimersTimer = false;
            cts2.Cancel();
            cts2.Dispose();
            gui.Destroy();
        }

        private static void StartEmulationThread(){
            if(_chip8.gameLoaded){
                Gui.menuBarVisible = false;
                StopEmulationThread();
                _chip8EmulationTimer = true;
                _cts = new CancellationTokenSource();
                ThreadPool.QueueUserWorkItem(new WaitCallback(EmulateChip8Cycle), _cts.Token);
            }
        }

        private static void StopEmulationThread(){
            if(_cts != null){
                _chip8EmulationTimer = false;
                _cts.Cancel();
                _cts.Dispose();
            }
        }

        private static void EmulateChip8Cycle(object obj){
            double targetDeltaMs = (double)1/Chip8.speedInHz*1000;
            DateTime lastTick = DateTime.Now;
            while(_chip8EmulationTimer){
                DateTime currentTick = DateTime.Now;
                TimeSpan span = currentTick - lastTick;
                if(span.TotalMilliseconds >= targetDeltaMs){
                  _chip8.EmulateCycle();
                  lastTick = currentTick;
                }
            }
        }

        private static void DecreaseChip8Timers(object obj){
            double targetDeltaMs = (double)1/60*1000;
            DateTime lastTick = DateTime.Now;
            while(_chip8TimersTimer){
                DateTime currentTick = DateTime.Now;
                TimeSpan span = currentTick - lastTick;
                if(_chip8.gameLoaded && (span.TotalMilliseconds >= targetDeltaMs)){
                    _chip8.DecreaseTimers();
                    lastTick = currentTick;
                }
            }
        }
    }
}
