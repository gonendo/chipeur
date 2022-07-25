using System;
using static SDL2.SDL;
using static SDL2.SDL_mixer;

namespace chipeur.sound
{
    class Sounds{
        private static bool _initialized;
        private static IntPtr _beep_sound;
        public static bool mute;

        public static void Initialize(){
            _initialized = SDL_WasInit(SDL_INIT_AUDIO) != 0;
            if(_initialized){
                if(Mix_OpenAudio(44100, MIX_DEFAULT_FORMAT, 2, 1024) == -1){
                    Console.WriteLine("Unable to initialize audio device : " + Mix_GetError());
                };
                _beep_sound = Mix_LoadWAV("assets/beep.wav");
            }
        }

        public static void Beep(){
            if(_initialized && !mute){
                if(Mix_PlayChannel(-1, _beep_sound, 0) == -1)
                    Console.WriteLine("Unable to play sound : " + Mix_GetError());
            }
        }

        public static void Destroy(){
            if(_initialized){
                Mix_FreeChunk(_beep_sound);
                Mix_CloseAudio();
            }
        }
    }
}