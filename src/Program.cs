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
        public const string VERSION = "3.0";
        private static bool _running = true;
        private static Chip8 _chip8;
        private static CancellationTokenSource _cts;
        private static CancellationTokenSource _cts2;
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
                LoadRom(romPath);
            };
            Gui.ChangeProfile += (int profileType) =>
            {
                _chip8.LoadProfile(profileType);
                if(_chip8.gameLoaded){
                    LoadRom(_chip8.gamePath);
                }
            };
            Chip8.StopEmulation += () =>
            {
                StopEmulationThread();
            };

            Graphics graphics = new Graphics();

            Sounds.Initialize();

            Input input = new Input();
            input.Initialize();

            _chip8 = new Chip8(input);
            _chip8.LoadProfile(Chip8.profile);
            _chip8.Initialize();

            if(args.Length > 0){
                _chip8.LoadGame(args[0]);
            }
            else{
                Gui.menuBarVisible = true;
            }

            StartEmulationThread();


            while(_running){
                if(_chip8.drawFlag){
                    _chip8.drawFlag = false;
                    graphics.Draw(_chip8.gfx);
                }

                if(_chip8.needToBeep){
                    Sounds.Beep();
                    _chip8.needToBeep = false;
                }

                gui.Update();
            }

            StopEmulationThread();
            gui.Destroy();
        }

        private static void LoadRom(string gamePath){
            StopEmulationThread();
            _chip8.Initialize();
            _chip8.LoadGame(gamePath);
            StartEmulationThread(false);
        }

        private static void StartEmulationThread(bool stopEmulation=true){
            if(_chip8.gameLoaded){
                Gui.menuBarVisible = false;
                if(stopEmulation){
                    StopEmulationThread();
                }
                _chip8EmulationTimer = true;
                _cts = new CancellationTokenSource();
                ThreadPool.QueueUserWorkItem(new WaitCallback(EmulateChip8Cycle), _cts.Token);

                _chip8TimersTimer = true;
                _cts2 = new CancellationTokenSource();
                ThreadPool.QueueUserWorkItem(new WaitCallback(UpdateChip8Timers), _cts2.Token);
            }
        }

        private static void StopEmulationThread(){
            if(_cts != null && _chip8EmulationTimer){
                _chip8EmulationTimer = false;
                _cts.Cancel();
                _cts.Dispose();
            }
            if(_cts2 != null && _chip8TimersTimer){
                _chip8TimersTimer = false;
                _cts2.Cancel();
                _cts2.Dispose();
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

        private static void UpdateChip8Timers(object obj){
            double targetDeltaMs = (double)1/60*1000;
            DateTime lastTick = DateTime.Now;
            while(_chip8TimersTimer){
                DateTime currentTick = DateTime.Now;
                TimeSpan span = currentTick - lastTick;
                if(_chip8.gameLoaded && (span.TotalMilliseconds >= targetDeltaMs)){
                    _chip8.UpdateTimers();
                    lastTick = currentTick;
                }
            }
        }
    }
}
