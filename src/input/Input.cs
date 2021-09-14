using System;
using static SDL2.SDL;
namespace chipeur.input
{
    class Input{
        public Byte[] key {get; set;}
        public SDL_Keycode[] keymap = new SDL_Keycode[16]{
            SDL_Keycode.SDLK_x,
            SDL_Keycode.SDLK_1,
            SDL_Keycode.SDLK_2,
            SDL_Keycode.SDLK_3,
            SDL_Keycode.SDLK_a,
            SDL_Keycode.SDLK_z,
            SDL_Keycode.SDLK_e,
            SDL_Keycode.SDLK_q,
            SDL_Keycode.SDLK_s,
            SDL_Keycode.SDLK_d,
            SDL_Keycode.SDLK_w,
            SDL_Keycode.SDLK_c,
            SDL_Keycode.SDLK_4,
            SDL_Keycode.SDLK_r,
            SDL_Keycode.SDLK_f,
            SDL_Keycode.SDLK_v
        };
        public Input(){

        }

        public void Initialize(){
            key = new Byte[16];
        }
    }
}