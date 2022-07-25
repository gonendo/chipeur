using System;
using System.Threading;
using chipeur.cpu;
using chipeur.graphics;
using chipeur.input;
using chipeur.sound;
using chipeur.gui;

namespace chipeur
{
    class Program
    {
        public const string VERSION = "2.0";
        private static bool _running = true;
        private static Chip8 _chip8;
        private static CancellationTokenSource _cts;
        private static PeriodicTimer _chip8EmulationTimer;
        private static PeriodicTimer _chip8TimersTimer;

        static void Main(string[] args)
        {
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
            _chip8TimersTimer.Dispose();
            cts2.Cancel();
            cts2.Dispose();
            gui.Destroy();
        }

        private static void StartEmulationThread(){
            if(_chip8.gameLoaded){
                Gui.menuBarVisible = false;
                StopEmulationThread();
                _cts = new CancellationTokenSource();
                ThreadPool.QueueUserWorkItem(new WaitCallback(EmulateChip8Cycle), _cts.Token);
            }
        }

        private static void StopEmulationThread(){
            if(_cts != null){
                _chip8EmulationTimer.Dispose();
                _cts.Cancel();
                _cts.Dispose();
            }
        }

        private static async void EmulateChip8Cycle(object obj){
            _chip8EmulationTimer = new PeriodicTimer(TimeSpan.FromMilliseconds((double)1/Chip8.speedInHz*1000));
            while(await _chip8EmulationTimer.WaitForNextTickAsync()){
                _chip8.EmulateCycle();
            }
        }

        private static async void DecreaseChip8Timers(object obj){
            _chip8TimersTimer = new PeriodicTimer(TimeSpan.FromMilliseconds((double)1/60*1000));
            while(await _chip8TimersTimer.WaitForNextTickAsync()){
                if(_chip8.gameLoaded){
                    _chip8.DecreaseTimers();
                }
            }
        }
    }
}
