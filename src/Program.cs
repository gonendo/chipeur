using System;
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
                SDL_Event e;
                if(SDL_PollEvent(out e)==1 && e.type == SDL_EventType.SDL_QUIT){
                    graphics.Destroy();
                    SDL_Quit();
                    break;
                }

                chip8.EmulateCycle();

                if(chip8.drawFlag){
                    graphics.Draw(chip8.gfx);
                }

                chip8.SetKeys();
            }
        }
    }
}
