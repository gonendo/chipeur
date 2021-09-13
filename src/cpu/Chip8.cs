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

        private delegate void InstructionDelegate(UInt16 opcode);
        private InstructionDelegate[] _delegates = new InstructionDelegate[16];
        private InstructionDelegate[] _0x0Delegates = new InstructionDelegate[15];
        private InstructionDelegate[] _0x8Delegates = new InstructionDelegate[15];
        private InstructionDelegate[] _0xEDelegates = new InstructionDelegate[15];
        private InstructionDelegate[] _0xFDelegates = new InstructionDelegate[102];

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

            //function pointers
            _delegates[0x0] = OpCode0x0;
            _0x0Delegates[0x0000] = CpuDisplayClear;
            _0x0Delegates[0x000E] = CpuReturn;
            _delegates[0x1] = CpuJump;
            _delegates[0x2] = CpuCallSubRoutine;
            _delegates[0x3] = CpuSkipIfVXEqualsNN;
            _delegates[0x4] = CpuSkipIfVXNotEqualsNN;
            _delegates[0x5] = CpuSkipIfVXEqualsVY;
            _delegates[0x6] = CpuSetVXToNN;
            _delegates[0x7] = CpuAddNNToVX;
            _delegates[0x8] = OpCode0x8;
            _0x8Delegates[0x0000] = CpuSetVXToVY;
            _0x8Delegates[0x0001] = CpuSetVXToVXOrVY;
            _0x8Delegates[0x0002] = CpuSetVXToVXAndVY;
            _0x8Delegates[0x0003] = CpuSetVXToVXXorVY;
            _0x8Delegates[0x0004] = CpuAddVYToVX;
            _0x8Delegates[0x0005] = CpuSubstractVYFromVX;
            _0x8Delegates[0x0006] = CpuStoreVXBitInVFAndShiftToRight;
            _0x8Delegates[0x0007] = CpuSetVXToVYMinusVX;
            _0x8Delegates[0x000E] = CpuStoreVXBitInVFAndShiftToLeft;
            _delegates[0x9] = CpuSkipIfVXNotEqualsVY;
            _delegates[0xA] = CpuSetIToNNN;
            _delegates[0xB] = CpuJumpToNNNPlusV0;
            _delegates[0xC] = CpuSetVXToBitwiseAndOnRandomAndNN;
            _delegates[0xD] = CpuDrawSpriteAtVXVY;
            _delegates[0xE] = OpCode0xE;
            _0xEDelegates[0x000E] = CpuSkipIfKeyPressed;
            _0xEDelegates[0x0001] = CpuSkipIfKeyNotPressed;
            _delegates[0xF] = OpCode0xF;
            _0xFDelegates[0x0007] = CpuSetVXToDelayTimerValue;
            _0xFDelegates[0x000A] = CpuWaitForKeyPressAndStoreInVX;
            _0xFDelegates[0x0015] = CpuSetDelayTimerToVX;
            _0xFDelegates[0x0018] = CpuSetSoundTimerToVX;
            _0xFDelegates[0x001E] = CpuAddVXToI;
            _0xFDelegates[0x0029] = CpuSetIToSpriteLocationInVX;
            _0xFDelegates[0x0033] = CpuStoreBCDOfVX;
            _0xFDelegates[0x0055] = CpuStoreV0ToVXAtAddressI;
            _0xFDelegates[0x0065] = CpuFillsV0ToVXWithValuesAtAddressI;
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
            //fetch opcode (2 bytes long)
            _opcode = (UInt16)(_memory[_pc] << 8 | _memory[_pc + 1]);
            
            //decode opcode
            UInt16 firstBits = (UInt16)(_opcode & 0xF000);
            Byte delegateIndex = (Byte)(firstBits >> 12);

            //execute opcode
            var func = _delegates[delegateIndex];
            Console.WriteLine(func.Method.Name+" "+_opcode.ToString("X4"));
            func(_opcode);

            if(_delay_timer > 0){
                --_delay_timer;
            }
            if(_sound_timer > 0){
                if(_sound_timer == 1){
                    //TODO BEEP
                }
                --_sound_timer;
            }
        }

        public void SetKeys(){

        }

        private void OpCode0x0(UInt16 opcode){
            _0x0Delegates[(opcode & 0x000F)](opcode);
        }

        //0x00E0 : Clears the screen
        private void CpuDisplayClear(UInt16 opcode){

        }

        //0x00EE : Returns from a subroutine
        private void CpuReturn(UInt16 opcode){

        }

        //0x1NNN : Jumps to address NNN
        private void CpuJump(UInt16 opcode){

        }

        //0x2NNN : Calls subroutine at NNN
        private void CpuCallSubRoutine(UInt16 opcode){

        }

        //0x3XNN : Skips the next instruction if VX equals NN. (Usually the next instruction is a jump to skip a code block);
        private void CpuSkipIfVXEqualsNN(UInt16 opcode){

        }

        //0x4XNN : Skips the next instruction if VX does not equal NN. (Usually the next instruction is a jump to skip a code block);
        private void CpuSkipIfVXNotEqualsNN(UInt16 opcode){

        }

        //0x5XY0 : Skips the next instruction if VX equals VY. (Usually the next instruction is a jump to skip a code block);
        private void CpuSkipIfVXEqualsVY(UInt16 opcode){

        }

        //0x6XNN : Sets VX to NN
        private void CpuSetVXToNN(UInt16 opcode){
            
        }

        //0x7XNN : Adds NN to VX. (Carry flag is not changed)
        private void CpuAddNNToVX(UInt16 opcode){

        }

        private void OpCode0x8(UInt16 opcode){
            _0x8Delegates[(opcode & 0x000F)](opcode);
        }

        //0x8XY0 : Sets VX to the value of VY.
        private void CpuSetVXToVY(UInt16 opcode){

        }

        //0x8XY1 : Sets VX to VX or VY. (Bitwise OR operation);
        private void CpuSetVXToVXOrVY(UInt16 opcode){
            
        }

        //0x8XY2 : Sets VX to VX and VY. (Bitwise AND operation);
        private void CpuSetVXToVXAndVY(UInt16 opcode){
            
        }

        //0x8XY3 : Sets VX to VX xor VY.
        private void CpuSetVXToVXXorVY(UInt16 opcode){
            
        }

        //0x8XY4 : Adds VY to VX. VF is set to 1 when there's a carry, and to 0 when there is not.
        private void CpuAddVYToVX(UInt16 opcode){
            
        }

        //0x8XY5 : VY is subtracted from VX. VF is set to 0 when there's a borrow, and 1 when there is not.
        private void CpuSubstractVYFromVX(UInt16 opcode){

        }

        //0x8XY6 : Stores the least significant bit of VX in VF and then shifts VX to the right by 1.
        private void CpuStoreVXBitInVFAndShiftToRight(UInt16 opcode){

        }

        //0x8XY7 : Sets VX to VY minus VX. VF is set to 0 when there's a borrow, and 1 when there is not.
        private void CpuSetVXToVYMinusVX(UInt16 opcode){

        }

        //0x8XYE : Stores the most significant bit of VX in VF and then shifts VX to the left by 1.
        private void CpuStoreVXBitInVFAndShiftToLeft(UInt16 opcode){

        }

        //0x9XY0 : Skips the next instruction if VX does not equal VY. (Usually the next instruction is a jump to skip a code block);
        private void CpuSkipIfVXNotEqualsVY(UInt16 opcode){

        }

        //0xANNN : Sets I to the address NNN.
        private void CpuSetIToNNN(UInt16 opcode){

        }

        //0xBNNN : Jumps to the address NNN plus V0.
        private void CpuJumpToNNNPlusV0(UInt16 opcode){

        }

        //0xCXNN : Sets VX to the result of a bitwise and operation on a random number (Typically: 0 to 255) and NN.
        private void CpuSetVXToBitwiseAndOnRandomAndNN(UInt16 opcode){

        }

        //0xDXYN : Draws a sprite at coordinate (VX, VY) that has a width of 8 pixels and a height of N pixels. Each row of 8 pixels is read as bit-coded starting from memory location I; I value does not change after the execution of this instruction. As described above, VF is set to 1 if any screen pixels are flipped from set to unset when the sprite is drawn, and to 0 if that does not happen
        private void CpuDrawSpriteAtVXVY(UInt16 opcode){

        }

        private void OpCode0xE(UInt16 opcode){
            _0xEDelegates[(opcode & 0x000F)](opcode);
        }

        //0xEX9E : Skips the next instruction if the key stored in VX is pressed. (Usually the next instruction is a jump to skip a code block);
        private void CpuSkipIfKeyPressed(UInt16 opcode){

        }

        //0xEXA1 : Skips the next instruction if the key stored in VX is not pressed. (Usually the next instruction is a jump to skip a code block);
        private void CpuSkipIfKeyNotPressed(UInt16 opcode){

        }

        private void OpCode0xF(UInt16 opcode){
            _0xFDelegates[(opcode & 0x00FF)](opcode);
        }

        //0xFX07 : Sets VX to the value of the delay timer.
        private void CpuSetVXToDelayTimerValue(UInt16 opcode){

        }

        //0xFX0A : A key press is awaited, and then stored in VX. (Blocking Operation. All instruction halted until next key event);
        private void CpuWaitForKeyPressAndStoreInVX(UInt16 opcode){

        }

        //0xFX15 : Sets the delay timer to VX.
        private void CpuSetDelayTimerToVX(UInt16 opcode){

        }

        //0xFX18 : Sets the sound timer to VX.
        private void CpuSetSoundTimerToVX(UInt16 opcode){

        }

        //0xFX1E : Adds VX to I. VF is not affected.
        private void CpuAddVXToI(UInt16 opcode){

        }

        //0xFX29 : Sets I to the location of the sprite for the character in VX. Characters 0-F (in hexadecimal) are represented by a 4x5 font.
        private void CpuSetIToSpriteLocationInVX(UInt16 opcode){

        }

        //0xFX33 : Stores the binary-coded decimal representation of VX, with the most significant of three digits at the address in I, the middle digit at I plus 1, and the least significant digit at I plus 2. (In other words, take the decimal representation of VX, place the hundreds digit in memory at location in I, the tens digit at location I+1, and the ones digit at location I+2.);
        private void CpuStoreBCDOfVX(UInt16 opcode){

        }

        //0xFX55 : Stores V0 to VX (including VX) in memory starting at address I. The offset from I is increased by 1 for each value written, but I itself is left unmodified.
        private void CpuStoreV0ToVXAtAddressI(UInt16 opcode){

        }

        //0xFX65 : Fills V0 to VX (including VX) with values from memory starting at address I. The offset from I is increased by 1 for each value written, but I itself is left unmodified.
        private void CpuFillsV0ToVXWithValuesAtAddressI(UInt16 opcode){

        }
    }
}