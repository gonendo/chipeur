using System;
using chipeur.cpu;

namespace chipeur.graphics
{
    class Graphics{
        public UInt32[] pixelsBuffer { get; private set;}
        public Graphics(){

        }

        public void Initialize(){
            pixelsBuffer = new UInt32[Chip8.DISPLAY_WIDTH * Chip8.DISPLAY_HEIGHT];
        }

        public void Draw(Byte[] gfxBuffer){
            for(int i=0; i < Chip8.DISPLAY_WIDTH * Chip8.DISPLAY_HEIGHT; i++){
                Byte pixel = gfxBuffer[i];
                pixelsBuffer[i] = (UInt32)((0x00FFFFFF * pixel) | 0xFF000000);
            }
        }


    }
}