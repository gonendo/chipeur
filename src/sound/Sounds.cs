using System;
using static SDL2.SDL_mixer;

namespace chipeur.sound
{
    class Sounds{
        private static IntPtr _beep_sound;

        public static void Initialize(){
            if(Mix_OpenAudio(44100, MIX_DEFAULT_FORMAT, 2, 1024) == -1){
                throw new Exception("Unable to initialize audio device : " + Mix_GetError());
            };
            
            _beep_sound = Mix_LoadWAV("assets/beep.wav");
        }

        public static void Beep(){
            if(Mix_PlayChannel(-1, _beep_sound, 0) == -1)
                throw new Exception("Unable to play sound : " + Mix_GetError());
        }

        public static void Destroy(){
            Mix_FreeChunk(_beep_sound);
            Mix_CloseAudio();
        }
    }
}