using System;
using static SDL2.SDL;
using chipeur.cpu;

namespace chipeur.graphics
{
    class Graphics{
        private const int WINDOW_WIDTH = 640;
        private const int WINDOW_HEIGHT = 480;
        private IntPtr _windowHandle;
        private IntPtr _renderer;
        private IntPtr _texture;
        private UInt32[] _pixelsBuffer;
        public Graphics(){

        }

        public int Initialize(){
            if(SDL_Init(SDL_INIT_VIDEO) < 0){
                return -1;
            }
            else{
                _windowHandle = SDL_CreateWindow("Chipeur - Chip8 Emulator", SDL_WINDOWPOS_CENTERED, SDL_WINDOWPOS_CENTERED, WINDOW_WIDTH, WINDOW_HEIGHT, 0);
                _renderer = SDL_CreateRenderer(_windowHandle, -1, 0);
                SDL_RenderSetLogicalSize(_renderer, WINDOW_WIDTH, WINDOW_HEIGHT);
                
                _texture = SDL_CreateTexture(_renderer, SDL_PIXELFORMAT_ABGR8888, (int)SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING, Chip8.DISPLAY_WIDTH, Chip8.DISPLAY_HEIGHT);
                _pixelsBuffer = new UInt32[Chip8.DISPLAY_WIDTH * Chip8.DISPLAY_HEIGHT];

                SDL_SetRenderDrawColor(_renderer, 0, 0, 0, 255);
                SDL_RenderClear(_renderer);
                SDL_RenderPresent(_renderer);
                return 0;
            }
        }

        public void Draw(Byte[] gfxBuffer){

        }

        public void Destroy(){
            SDL_DestroyRenderer(_renderer);
            SDL_DestroyWindow(_windowHandle);
        }

        public string GetError(){
            return SDL_GetError();
        }
    }
}