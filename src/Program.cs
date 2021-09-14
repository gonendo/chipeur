using System;
using System.Threading;
using static SDL2.SDL;
using chipeur.cpu;
using chipeur.graphics;
using chipeur.input;

namespace chipeur
{
    class Program
    {
        static void Main(string[] args)
        {
            if(args.Length==0){
                throw new ArgumentException("Missing game path");
            }

            Chip8 chip8 = new Chip8();
            Graphics graphics = new Graphics();
            if(graphics.Initialize() < 0){
                throw new Exception("Graphics couldn't initialize:\n"+graphics.GetError());
            }

            Input input = new Input();
            input.Initialize();

            chip8.Initialize(input);
            chip8.LoadGame(args[0]);

            while(true){
                chip8.EmulateCycle();

                SDL_Event e;
                if(SDL_PollEvent(out e) == 1){
                    switch(e.type){
                        case SDL_EventType.SDL_QUIT:
                            graphics.Destroy();
                            SDL_Quit();
                            return;
                        case SDL_EventType.SDL_KEYDOWN:
                            for(int i=0; i < input.keymap.Length; i++){
                                if(e.key.keysym.sym == input.keymap[i]){
                                    input.key[i] = 1;
                                }
                            }
                            break;
                        case SDL_EventType.SDL_KEYUP:
                            for(int i=0; i < input.keymap.Length; i++){
                                if(e.key.keysym.sym == input.keymap[i]){
                                    input.key[i] = 0;
                                }
                            }
                            break;
                    }
                }

                if(chip8.drawFlag){
                    chip8.drawFlag = false;
                    graphics.Draw(chip8.gfx);
                }

                Thread.Sleep(1);
            }
        }
    }
}
