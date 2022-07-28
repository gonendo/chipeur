using System;
using chipeur.cpu;

namespace chipeur.graphics
{
    class Graphics{
        public static UInt32[] pixelsBuffer;
        public Graphics(){
            Chip8.ChangeDisplayResolution += (int displayWidth, int displayHeight) =>
            {
                pixelsBuffer = new UInt32[displayWidth * displayHeight];
            };
        }

        public void Draw(Byte[] gfxBuffer){
            for(int i=0; i < Chip8.displayWidth * Chip8.displayHeight; i++){
                Byte pixel = gfxBuffer[i];
                pixelsBuffer[i] = (UInt32)((0x00FFFFFF * pixel) | 0xFF000000);
            }
        }


    }
}