using System;
using static SDL2.SDL;
namespace chipeur.input
{
    class Input{
        public Byte[] key {get; set;}
        public Input(){

        }

        public void Initialize(){
            key = new Byte[16];
        }
    }
}