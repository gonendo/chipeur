using System;
using System.IO;

namespace chipeur.cpu
{
    class Chip8{
        public const int DISPLAY_WIDTH = 64;
        public const int DISPLAY_HEIGHT = 32;
        private const int MEMORY_SIZE = 4096;
        private const int MEMORY_PROGRAM_START = 0x200;

        public bool drawFlag {get; private set;}
        public Byte[] gfx {get; private set;}
        private Byte[] _memory;
        private UInt16[] _stack;
        private Byte[] _V; //registers V0-VE

        private UInt16 _pc; //program counter
        private UInt16 _opcode;
        private UInt16 _I; //index register
        private UInt16 _sp; //stack pointer

        private Byte _sound_timer;
        private Byte _delay_timer;

        private Byte[] _chip8_fontset = new Byte[80]
        {
            0xF0, 0x90, 0x90, 0x90, 0xF0, // 0
            0x20, 0x60, 0x20, 0x20, 0x70, // 1
            0xF0, 0x10, 0xF0, 0x80, 0xF0, // 2
            0xF0, 0x10, 0xF0, 0x10, 0xF0, // 3
            0x90, 0x90, 0xF0, 0x10, 0x10, // 4
            0xF0, 0x80, 0xF0, 0x10, 0xF0, // 5
            0xF0, 0x80, 0xF0, 0x90, 0xF0, // 6
            0xF0, 0x10, 0x20, 0x40, 0x40, // 7
            0xF0, 0x90, 0xF0, 0x90, 0xF0, // 8
            0xF0, 0x90, 0xF0, 0x10, 0xF0, // 9
            0xF0, 0x90, 0xF0, 0x90, 0x90, // A
            0xE0, 0x90, 0xE0, 0x90, 0xE0, // B
            0xF0, 0x80, 0x80, 0x80, 0xF0, // C
            0xE0, 0x90, 0x90, 0x90, 0xE0, // D
            0xF0, 0x80, 0xF0, 0x80, 0xF0, // E
            0xF0, 0x80, 0xF0, 0x80, 0x80  // F
        };

        public Chip8(){

        }

        public void Initialize(){
            _pc = MEMORY_PROGRAM_START;
            _opcode = 0;
            _I = 0;
            _sp = 0;

            gfx = new Byte[DISPLAY_WIDTH * DISPLAY_HEIGHT];
            _memory = new Byte[MEMORY_SIZE];
            _stack = new UInt16[16];
            _V = new Byte[16];

            //load fontset
            for(int i=0; i < 80; i++){
                _memory[i] = _chip8_fontset[i];
            }

            _sound_timer = 0;
            _delay_timer = 0;
        }

        public void LoadGame(string gamePath){
            Console.WriteLine("Loading game "+gamePath);
            using var binaryReader = new BinaryReader(File.Open(gamePath, FileMode.Open));
            if(binaryReader.BaseStream.Length < MEMORY_SIZE - MEMORY_PROGRAM_START){
                long position=0;
                do{
                    _memory[position + MEMORY_PROGRAM_START] = binaryReader.ReadByte();
                    position++;
                }
                while(position < binaryReader.BaseStream.Length);
                binaryReader.Close();
            }
            else{
                throw new Exception("Invalid rom size");
            }
        }

        public void EmulateCycle(){

        }

        public void SetKeys(){

        }
    }
}