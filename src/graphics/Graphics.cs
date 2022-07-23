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

        public void Initialize(){
            _windowHandle = SDL_CreateWindow("Chipeur - Chip8 Emulator", SDL_WINDOWPOS_CENTERED, SDL_WINDOWPOS_CENTERED, WINDOW_WIDTH, WINDOW_HEIGHT, 0);
            _renderer = SDL_CreateRenderer(_windowHandle, -1, 0);
            SDL_RenderSetLogicalSize(_renderer, WINDOW_WIDTH, WINDOW_HEIGHT);
            
            _texture = SDL_CreateTexture(_renderer, SDL_PIXELFORMAT_ABGR8888, (int)SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING, Chip8.DISPLAY_WIDTH, Chip8.DISPLAY_HEIGHT);
            _pixelsBuffer = new UInt32[Chip8.DISPLAY_WIDTH * Chip8.DISPLAY_HEIGHT];

            SDL_SetRenderDrawColor(_renderer, 0, 0, 0, 255);
            SDL_RenderClear(_renderer);
            SDL_RenderPresent(_renderer);
        }

        public void Draw(Byte[] gfxBuffer){
            for(int i=0; i < Chip8.DISPLAY_WIDTH * Chip8.DISPLAY_HEIGHT; i++){
                Byte pixel = gfxBuffer[i];
                _pixelsBuffer[i] = (UInt32)((0x00FFFFFF * pixel) | 0xFF000000);
            }
            unsafe
            {
                fixed (UInt32* pixels = _pixelsBuffer)
                {
                    SDL_UpdateTexture(_texture, IntPtr.Zero, (IntPtr)pixels, Chip8.DISPLAY_WIDTH * sizeof(UInt32));
                }
            }
            SDL_RenderClear(_renderer);
            SDL_RenderCopy(_renderer, _texture, IntPtr.Zero, IntPtr.Zero);
            SDL_RenderPresent(_renderer);
        }

        public void Destroy(){
            SDL_DestroyRenderer(_renderer);
            SDL_DestroyWindow(_windowHandle);
        }

    }
}