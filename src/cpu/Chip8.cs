using System;
using System.IO;
using chipeur.input;

namespace chipeur.cpu
{
    class Chip8{
        public static int speedInHz = 500;
        private const int MEMORY_SIZE = 4096;
        private const int MEMORY_PROGRAM_START = 0x200;

        public const int PROFILE_CHIP8 = 0;
        public const int PROFILE_SUPERCHIP = 1;

        public static event Action<int,int> ChangeDisplayResolution;
        public static event Action StopEmulation;

        public bool drawFlag {get; set;}
        public bool needToBeep {get; set;}
        public bool gameLoaded {get {return _gamePath!=null;}}
        public Byte[] gfx {get; private set;}
        private Byte[] _memory;
        private UInt16[] _stack;
        private Byte[] _V; //registers V0-VE
        private Byte[] _userFlags;

        private UInt16 _pc; //program counter
        private UInt16 _opcode;
        private UInt16 _I; //index register
        private UInt16 _sp; //stack pointer

        private Byte _sound_timer;
        private Byte _delay_timer;
        private int _waitForInterrupt;

        private string _gamePath;
        public string  gamePath {get {return _gamePath;}}

        private Input _input;

        public static byte displayWidth;
        public static byte displayHeight;
        public static bool hiResMode = false;

        private static bool vf_reset_quirk;
        private static bool mem_quirk;
        private static bool display_wait_quirk;
        private static bool clipping_quirk;
        private static bool shifting_quirk;
        private static bool jumping_quirk;

        public readonly struct Profile
        {
            public Profile(string name, byte displayWidth, byte displayHeight, bool vfResetQuirk, bool memQuirk, bool displayWaitQuirk, bool clippingQuirk, bool shiftingQuirk, bool jumpingQuirk){
                this.name = name;
                this.displayWidth = displayWidth;
                this.displayHeight = displayHeight;
                this.vfResetQuirk = vfResetQuirk;
                this.memQuirk = memQuirk;
                this.displayWaitQuirk = displayWaitQuirk;
                this.clippingQuirk = clippingQuirk;
                this.shiftingQuirk = shiftingQuirk;
                this.jumpingQuirk = jumpingQuirk;
            }
            public string name {get; init;}
            public byte displayWidth {get; init;}
            public byte displayHeight {get; init;}
            public bool vfResetQuirk {get; init;}
            public bool memQuirk {get; init;}
            public bool displayWaitQuirk {get; init;}
            public bool clippingQuirk {get; init;}
            public bool shiftingQuirk {get; init;}
            public bool jumpingQuirk {get; init;}
        }

        private Profile[] _profiles;
        public static int profile;

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
        private Byte[] _schip_fontset = new Byte[100]
        {
            0x3C, 0x7E, 0xC3, 0xC3, 0xC3, 0xC3, 0xC3, 0xC3, 0x7E, 0x3C, // 0
            0x18, 0x38, 0x58, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x3C, // 1
            0x3E, 0x7F, 0xC3, 0x06, 0x0C, 0x18, 0x30, 0x60, 0xFF, 0xFF, // 2
            0x3C, 0x7E, 0xC3, 0x03, 0x0E, 0x0E, 0x03, 0xC3, 0x7E, 0x3C, // 3
            0x06, 0x0E, 0x1E, 0x36, 0x66, 0xC6, 0xFF, 0xFF, 0x06, 0x06, // 4
            0xFF, 0xFF, 0xC0, 0xC0, 0xFC, 0xFE, 0x03, 0xC3, 0x7E, 0x3C, // 5
            0x3E, 0x7C, 0xC0, 0xC0, 0xFC, 0xFE, 0xC3, 0xC3, 0x7E, 0x3C, // 6
            0xFF, 0xFF, 0x03, 0x06, 0x0C, 0x18, 0x30, 0x60, 0x60, 0x60, // 7
            0x3C, 0x7E, 0xC3, 0xC3, 0x7E, 0x7E, 0xC3, 0xC3, 0x7E, 0x3C, // 8
            0x3C, 0x7E, 0xC3, 0xC3, 0x7F, 0x3F, 0x03, 0x03, 0x3E, 0x7C  // 9
        };

        private delegate void InstructionDelegate(UInt16 opcode);
        private InstructionDelegate[] _delegates = new InstructionDelegate[16];
        private InstructionDelegate[] _0x0Delegates = new InstructionDelegate[561];
        private InstructionDelegate[] _0x8Delegates = new InstructionDelegate[15];
        private InstructionDelegate[] _0xEDelegates = new InstructionDelegate[15];
        private InstructionDelegate[] _0xFDelegates = new InstructionDelegate[134];

        public Chip8(Input input){
            _input = input;

            //function pointers
            _delegates[0x0] = OpCode0x0;
            _0x0Delegates[0x00E0] = CpuDisplayClear;
            _0x0Delegates[0x00EE] = CpuReturn;
            _0x0Delegates[0x00FB] = CpuScrollDisplayRight;
            _0x0Delegates[0x00FC] = CpuScrollDisplayLeft;
            _0x0Delegates[0x00FD] = CpuExitInterpreter;
            _0x0Delegates[0x00FE] = CpuDisableHighResMode;
            _0x0Delegates[0x00FF] = CpuEnableHighResMode;
            _0x0Delegates[0x0230] = CpuDisplayClear;
            _delegates[0x1] = OpCode0x1;
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
            _delegates[0xB] = CpuJumpToNNNPlusVX;
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
            _0xFDelegates[0x0030] = CpuSetITo10byteSpriteLocationInVX;
            _0xFDelegates[0x0033] = CpuStoreBCDOfVX;
            _0xFDelegates[0x0055] = CpuStoreV0ToVXAtAddressI;
            _0xFDelegates[0x0065] = CpuFillV0ToVXWithValuesAtAddressI;
            _0xFDelegates[0x0075] = CpuStoreV0ToVXInRPL;
            _0xFDelegates[0x0085] = CpuReadV0ToVXFromRPL;

            var chip8Profile = new Profile("Chip 8 (Cosmac VIP)", 64, 32, true, true, true, true, false, false);
            var superChipProfile = new Profile("SuperChip 1.1", 128, 64, false, false, false, true, true, true);
            _profiles = new Profile[] {chip8Profile, superChipProfile};
            profile = PROFILE_CHIP8;
        }

        public void Initialize(){
            _pc = MEMORY_PROGRAM_START;
            _opcode = 0;
            _I = 0;
            _sp = 0;

            _memory = new Byte[MEMORY_SIZE];
            _stack = new UInt16[16];
            _V = new Byte[16];
            _userFlags = new Byte[8];

            //load fontset
            for(int i=0; i < _chip8_fontset.Length; i++){
                _memory[i] = _chip8_fontset[i];
            }
            if(profile == PROFILE_SUPERCHIP){
                for(int i=_chip8_fontset.Length; i < _chip8_fontset.Length + _schip_fontset.Length; i++){
                    _memory[i] = _schip_fontset[i-_chip8_fontset.Length];
                }
            }

            RestoreDisplayResolutionFromProfile(profile);
            _sound_timer = 0;
            _delay_timer = 0;
            _waitForInterrupt = 0;
            hiResMode = false;
        }

        public void LoadProfile(int profileType){
            RestoreDisplayResolutionFromProfile(profileType);
            var profile = _profiles[profileType];
            vf_reset_quirk = profile.vfResetQuirk;
            mem_quirk = profile.memQuirk;
            display_wait_quirk = profile.displayWaitQuirk;
            clipping_quirk = profile.clippingQuirk;
            shifting_quirk = profile.shiftingQuirk;
            jumping_quirk = profile.jumpingQuirk;
            Chip8.profile = profileType;
        }

        private void RestoreDisplayResolutionFromProfile(int profileType){
            var profile = _profiles[profileType];
            gfx = new Byte[profile.displayWidth * profile.displayHeight];
            ChangeDisplayResolution.Invoke(profile.displayWidth, profile.displayHeight);
            displayWidth = profile.displayWidth;
            displayHeight = profile.displayHeight;
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
            _gamePath = gamePath;
        }

        public void EmulateCycle(){
            //fetch opcode (2 bytes long)
            _opcode = (UInt16)(_memory[_pc] << 8 | _memory[_pc + 1]);
            
            //decode opcode
            UInt16 firstBits = (UInt16)(_opcode & 0xF000);
            Byte delegateIndex = (Byte)(firstBits >> 12);

            //execute opcode
            var func = delegateIndex >=0 && delegateIndex < _delegates.Length ? _delegates[delegateIndex] : null;
            //Console.WriteLine(_pc+" "+func.Method.Name+" "+_opcode.ToString("X4"));
            if(func != null){
                func(_opcode);
            }
            else{
                _pc += 2;
            }
        }

        public void UpdateTimers(){
            if(_delay_timer > 0){
                --_delay_timer;
            }

            if(_sound_timer > 0){
                if(_sound_timer == 1){
                    needToBeep = true;
                }
                --_sound_timer;
            }

            if(_waitForInterrupt == 1){
                _waitForInterrupt = 2;
            }
        }

        private bool waitForInterrupt(){
            switch (_waitForInterrupt){
                case 0:
                    _waitForInterrupt = 1;
                    _pc -= 2;
                    return true;
                case 1:
                    _pc -= 2;
                    return true;
                default:
                    _waitForInterrupt = 0;
                    return false;
            }
        }

        private void OpCode0x0(UInt16 opcode){
            //0x00CN
            if((opcode & 0x00F0) >> 4 == 0xC){
                CpuScrollDisplay(opcode);
            }
            else{
                var delegateIndex = (opcode & 0x0FFF);
                var func = delegateIndex >=0 && delegateIndex < _0x0Delegates.Length ? _0x0Delegates[delegateIndex] : null;
                //Console.WriteLine(_pc+" "+func.Method.Name+" "+opcode.ToString("X4"));
                if(func != null){
                    func(opcode);
                }
                else{
                    _pc += 2;
                }
            }
        }

        //0x00CN : Scroll display N pixels down; in low resolution mode, N/2 pixels
        private void CpuScrollDisplay(UInt16 opcode){
            byte n = (byte)(opcode & 0x000F);
            var offset = displayWidth * n;
            for(int i = displayWidth * displayHeight; i > 0; i--){
                int j = i - 1;
                byte pixel = 0;
                if (j > offset){
                    pixel = gfx[j - offset];
                }
                gfx[j] = pixel;
            }

            drawFlag = true;
            _pc += 2;
        }

        //0x00E0 : Clears the screen
        private void CpuDisplayClear(UInt16 opcode){
            for(int i=0; i < displayWidth*displayHeight; i++){
                gfx[i] = 0;
            }

            drawFlag = true;
            _pc += 2;
        }

        //0x00EE : Returns from a subroutine
        private void CpuReturn(UInt16 opcode){
            --_sp;
            _pc = _stack[_sp];
            _pc += 2;
        }

        //0x00FB : Scroll right by 4 pixels; in low resolution mode, 2 pixels
        private void CpuScrollDisplayRight(UInt16 opcode){
            for(int i = displayWidth * displayHeight; i > 0; i--){
                int j = i - 1;
                byte pixel = 0;
                if(j % displayWidth >= 4){
                    pixel = gfx[j - 4];
                }
                gfx[j] = pixel;
            }

            drawFlag = true;
            _pc += 2;
        }

        //0x00FC : Scroll left by 4 pixels; in low resolution mode, 2 pixels
        private void CpuScrollDisplayLeft(UInt16 opcode){
            for(int i=0; i < displayWidth * displayHeight; i++){
                byte pixel = 0;
                if(i % displayWidth < displayWidth - 4){
                    pixel = gfx[i + 4];
                }
                gfx[i] = pixel;
            }

            drawFlag = true;
            _pc += 2;
        }

        //0x00FD : Exit interpreter
        private void CpuExitInterpreter(UInt16 opcode){
            StopEmulation.Invoke();
        }

        //0x00FE : Disable high-resolution mode
        private void CpuDisableHighResMode(UInt16 opcode){
            hiResMode = false;
            _pc += 2;
        }

        //0x00FF : Enable high-resolution mode
        private void CpuEnableHighResMode(UInt16 opcode){
            hiResMode = true;
            _pc += 2;
        }

        //0x1NNN
        private void OpCode0x1(UInt16 opcode){
            //0x1260 only if it's the first instruction : set 2-page hires mode (64x64) and jump to 0x2c0
            if(_pc == MEMORY_PROGRAM_START && opcode == 0x1260){
                ChangeDisplayResolution.Invoke(64, 64);
                displayWidth = 64;
                displayHeight = 64;
                gfx = new Byte[displayWidth * displayHeight];
                hiResMode = true;
                _pc = 0x02c0;
            }
            else{
                //jump to address NNN
                _pc = (UInt16)(opcode & 0x0fff);
            }
        }

        //0x2NNN : Calls subroutine at NNN
        private void CpuCallSubRoutine(UInt16 opcode){
            //store the current program counter address in the stack
            _stack[_sp] = _pc;
            ++_sp;
            //jump to address NNN
            _pc = (UInt16)(opcode & 0x0fff);
        }

        //0x3XNN : Skips the next instruction if VX equals NN. (Usually the next instruction is a jump to skip a code block);
        private void CpuSkipIfVXEqualsNN(UInt16 opcode){
            if(_V[(opcode & 0x0f00) >> 8] == (opcode & 0x00ff)){
                _pc += 4;
            }
            else{
                _pc += 2;
            }
        }

        //0x4XNN : Skips the next instruction if VX does not equal NN. (Usually the next instruction is a jump to skip a code block);
        private void CpuSkipIfVXNotEqualsNN(UInt16 opcode){
            if(_V[(opcode & 0x0f00) >> 8] != (opcode & 0x00ff)){
                _pc += 4;
            }
            else{
                _pc += 2;
            }
        }

        //0x5XY0 : Skips the next instruction if VX equals VY. (Usually the next instruction is a jump to skip a code block);
        private void CpuSkipIfVXEqualsVY(UInt16 opcode){
            if(_V[(opcode & 0x0f00) >> 8] == _V[(opcode & 0x00f0) >> 4]){
                _pc += 4;
            }
            else{
                _pc += 2;
            }
        }

        //0x6XNN : Sets VX to NN
        private void CpuSetVXToNN(UInt16 opcode){
            _V[(opcode & 0x0f00) >> 8] = (Byte)(opcode & 0x00ff);
            _pc += 2;
        }

        //0x7XNN : Adds NN to VX. (Carry flag is not changed)
        private void CpuAddNNToVX(UInt16 opcode){
            _V[(opcode & 0x0f00) >> 8] += (Byte)(opcode & 0x00ff);
            _pc += 2;
        }

        private void OpCode0x8(UInt16 opcode){
            var delegateIndex = (opcode & 0x000F);
            var func = delegateIndex >=0 && delegateIndex < _0x8Delegates.Length ? _0x8Delegates[delegateIndex] : null;
            //Console.WriteLine(_pc+" "+func.Method.Name+" "+opcode.ToString("X4"));
            if(func != null){
                func(opcode);
            }
            else{
                _pc += 2;
            }
        }

        //0x8XY0 : Sets VX to the value of VY.
        private void CpuSetVXToVY(UInt16 opcode){
            _V[(opcode & 0x0f00) >> 8] = _V[(opcode & 0x00f0) >> 4];
            _pc += 2;
        }

        //0x8XY1 : Sets VX to VX or VY. (Bitwise OR operation);
        private void CpuSetVXToVXOrVY(UInt16 opcode){
            _V[(opcode & 0x0f00) >> 8] |= _V[(opcode & 0x00f0) >> 4];
            if(vf_reset_quirk)
                _V[0xf] = 0;
            _pc += 2;
        }

        //0x8XY2 : Sets VX to VX and VY. (Bitwise AND operation);
        private void CpuSetVXToVXAndVY(UInt16 opcode){
            _V[(opcode & 0x0f00) >> 8] &= _V[(opcode & 0x00f0) >> 4];
            if(vf_reset_quirk)
                _V[0xf] = 0;
            _pc += 2;
        }

        //0x8XY3 : Sets VX to VX xor VY.
        private void CpuSetVXToVXXorVY(UInt16 opcode){
            _V[(opcode & 0x0f00) >> 8] ^= _V[(opcode & 0x00f0) >> 4];
            if(vf_reset_quirk)
                _V[0xf] = 0;
            _pc += 2;
        }

        //0x8XY4 : Adds VY to VX. VF is set to 1 when there's a carry, and to 0 when there is not.
        private void CpuAddVYToVX(UInt16 opcode){
            var sum = _V[(opcode & 0x0f00) >> 8] + _V[(opcode & 0x00f0) >> 4];
            _V[(opcode & 0x0f00) >> 8] = (Byte)(sum > 0xff ? sum - (0xff+1) : sum);
            _V[0xf] = (Byte)(sum > 0xff ? 1 : 0);
            _pc += 2;
        }

        //0x8XY5 : VY is subtracted from VX. VF is set to 0 when there's a borrow, and 1 when there is not.
        private void CpuSubstractVYFromVX(UInt16 opcode){
            if(_V[(opcode & 0x0f00) >> 8] > _V[(opcode & 0x00f0) >> 4]){
                _V[(opcode & 0x0f00) >> 8] -= _V[(opcode & 0x00f0) >> 4];
                _V[0xf] = 1;
            }
            else{
                _V[(opcode & 0x0f00) >> 8] = (Byte)((0xff+1) + _V[(opcode & 0x0f00) >> 8] - _V[(opcode & 0x00f0) >> 4]);
                _V[0xf] = 0;
            }
            _pc += 2;
        }

        //0x8XY6 : Stores the least significant bit of VX in VF and then shifts VX to the right by 1.
        private void CpuStoreVXBitInVFAndShiftToRight(UInt16 opcode){
            if(!shifting_quirk){
                var VY = _V[(opcode & 0x00f0) >> 4];
                _V[(opcode & 0x0f00) >> 8] = VY;
            }
            var bit = (Byte)(_V[(opcode & 0x0f00) >> 8] & 1);
            _V[(opcode & 0x0f00) >> 8] >>= 1;
            _V[0xf] = (Byte)(bit > 0 ? 1 : 0);
            _pc += 2;
        }

        //0x8XY7 : Sets VX to VY minus VX. VF is set to 0 when there's a borrow, and 1 when there is not.
        private void CpuSetVXToVYMinusVX(UInt16 opcode){
            if(_V[(opcode & 0x0f00) >> 8] > _V[(opcode & 0x00f0) >> 4]){
                _V[(opcode & 0x0f00) >> 8] = (Byte)((0xff+1) + _V[(opcode & 0x00f0) >> 4] - _V[(opcode & 0x0f00) >> 8]);
                _V[0xf] = 0;
            }
            else{
                _V[(opcode & 0x0f00) >> 8] = (Byte)(_V[(opcode & 0x00f0) >> 4] - _V[(opcode & 0x0f00) >> 8]);
                _V[0xf] = 1;
            }
            _pc += 2;
        }

        //0x8XYE : Stores the most significant bit of VX in VF and then shifts VX to the left by 1.
        private void CpuStoreVXBitInVFAndShiftToLeft(UInt16 opcode){
            if(!shifting_quirk){
                var VY = _V[(opcode & 0x00f0) >> 4];
                _V[(opcode & 0x0f00) >> 8] = VY;
            }
            _V[(opcode & 0x0f00) >> 8] <<= 1;
            _V[0xf] = (Byte)(_V[(opcode & 0x0f00) >> 8] >> 7);
            _pc += 2;
        }

        //0x9XY0 : Skips the next instruction if VX does not equal VY. (Usually the next instruction is a jump to skip a code block);
        private void CpuSkipIfVXNotEqualsVY(UInt16 opcode){
            if(_V[(opcode & 0x0f00) >> 8] != _V[(opcode & 0x00f0) >> 4]){
                _pc += 4;
            }
            else{
                _pc += 2;
            }
        }

        //0xANNN : Sets I to the address NNN.
        private void CpuSetIToNNN(UInt16 opcode){
            _I = (UInt16)(opcode & 0x0fff);
            _pc += 2;
        }

        //0xBNNN : Jumps to the address NNN plus V0 (or 0xBXNN XNN + VX where X is the highest nibble on NNN).
        private void CpuJumpToNNNPlusVX(UInt16 opcode){
            if(!jumping_quirk){
                _pc = (UInt16)((opcode & 0x0fff) + _V[0]);
            }
            else{
                _pc = (UInt16)((opcode & 0x0fff) + _V[(opcode & 0x0f00) >> 8]);
            }
        }

        //0xCXNN : Sets VX to the result of a bitwise and operation on a random number (Typically: 0 to 255) and NN.
        private void CpuSetVXToBitwiseAndOnRandomAndNN(UInt16 opcode){
            Random r = new Random();
            _V[(opcode & 0x0f00) >> 8] = (Byte)(r.Next(0xFF+1) & (opcode & 0x00ff));
            _pc += 2;
        }

        /*0xDXYN : Draws a sprite at coordinate (VX, VY) that has a width of 8 pixels and a height of N pixels. 
        Each row of 8 pixels is read as bit-coded starting from memory location I; 
        I value does not change after the execution of this instruction. 
        As described above, VF is set to 1 if any screen pixels are flipped from set to unset when the sprite is drawn, and to 0 if that does not happen*/
        private void CpuDrawSpriteAtVXVY(UInt16 opcode){
            if(display_wait_quirk && waitForInterrupt()){
                _pc += 2;
                return;
            }
            //Get the position from the registers
            byte x = _V[(opcode & 0x0f00) >> 8];
            byte y = _V[(opcode & 0x00f0) >> 4];

            //Number of lines of 8 pixels width
            byte n = (byte)(opcode & 0x000f);

            if((profile == PROFILE_SUPERCHIP) && hiResMode && (n == 0)){
                n = 16;
            }

            byte width = (byte) (profile == PROFILE_SUPERCHIP && !hiResMode ? 64 : displayWidth);
            byte height = (byte) (profile == PROFILE_SUPERCHIP && !hiResMode ? 32 : displayHeight);

            for(int i=x; i >= width; i -= width){
                x -= width;
            }

            for(int i=y; i >= height; i -= height){
                y -= height;
            }

            bool erases = false;

            //Iterate through each of the sprite lines starting at address I
            var offset = 0;
            for(int i=0; i < n; i++){
                var line = _memory[_I + offset];
                if(y + i >= height){
                    if(clipping_quirk){
                        break;
                    }
                }
                for(int j=0; j < 8; j++){
                    if(x + j >= width){
                        if(clipping_quirk){
                            break;
                        }
                    }
                    if((line & (0x80 >> j)) != 0){
                        if(profile == PROFILE_SUPERCHIP && !hiResMode){
                            for(int k=0; k <= 1; k++){
                                for(int l=0; l <=1; l++){
                                    var px = x*2+k + j*2 + ((y*2+l + i*2) * displayWidth);
                                    erases = erases || gfx[px] == 1;
                                    gfx[px] ^= 1;
                                }
                            }
                        }
                        else{
                            var pixelIndex =  x + j + ((y + i) * width);
                            erases = erases || gfx[pixelIndex] == 1;
                            gfx[pixelIndex] ^= 1;
                        }
                    }
                }
                offset++;
                if (n == 16){
                    line = _memory[_I + offset];
                    for(int j=0; j < 8; j++){
                        if((line & (0x80 >> j)) != 0){
                            var pixelIndex =  x + j + ((y + i) * width);
                            erases = erases || gfx[pixelIndex + 8] == 1;
                            gfx[pixelIndex + 8] ^= 1;
                        }
                    }
                    offset++;
                }
            }

            _V[0xf] = (byte)(erases ? 1 : 0);

            drawFlag = true;
            _pc += 2;
        }

        private void OpCode0xE(UInt16 opcode){
            var delegateIndex = (opcode & 0x000F);
            var func = delegateIndex >=0 && delegateIndex < _0xEDelegates.Length ? _0xEDelegates[delegateIndex] : null;
            //Console.WriteLine(_pc+" "+func.Method.Name+" "+opcode.ToString("X4"));
            if(func != null){
                func(opcode);
            }
            else{
                _pc += 2;
            }
        }

        //0xEX9E : Skips the next instruction if the key stored in VX is pressed. (Usually the next instruction is a jump to skip a code block);
        private void CpuSkipIfKeyPressed(UInt16 opcode){
            if(_input.key[_V[(opcode & 0x0f00) >> 8]] != 0){
                _pc += 4;
            }
            else{
                _pc += 2;
            }
        }

        //0xEXA1 : Skips the next instruction if the key stored in VX is not pressed. (Usually the next instruction is a jump to skip a code block);
        private void CpuSkipIfKeyNotPressed(UInt16 opcode){
            if(_input.key[_V[(opcode & 0x0f00) >> 8]] == 0){
                _pc += 4;
            }
            else{
                _pc += 2;
            }
        }

        private void OpCode0xF(UInt16 opcode){
            var delegateIndex = (opcode & 0x00FF);
            var func = delegateIndex >=0 && delegateIndex < _0xFDelegates.Length ? _0xFDelegates[delegateIndex] : null;
            //Console.WriteLine(_pc+" "+func.Method.Name+" "+opcode.ToString("X4"));
            if(func != null){
                func(opcode);
            }
            else{
                _pc += 2;
            }
        }

        //0xFX07 : Sets VX to the value of the delay timer.
        private void CpuSetVXToDelayTimerValue(UInt16 opcode){
            _V[(opcode & 0x0f00) >> 8] = _delay_timer;
            _pc += 2;
        }

        //0xFX0A : A key press is awaited, and then stored in VX. (Blocking Operation. All instruction halted until next key event);
        private void CpuWaitForKeyPressAndStoreInVX(UInt16 opcode){
            for(int i=0; i < _input.key.Length; i++){
                if(_input.key[i]!=0){
                    _V[(opcode & 0x0f00) >> 8] = (Byte)i;
                    _pc += 2;
                    return;
                }
            }
        }

        //0xFX15 : Sets the delay timer to VX.
        private void CpuSetDelayTimerToVX(UInt16 opcode){
            _delay_timer = _V[(opcode & 0x0f00) >> 8];
            _pc += 2;
        }

        //0xFX18 : Sets the sound timer to VX.
        private void CpuSetSoundTimerToVX(UInt16 opcode){
            _sound_timer = _V[(opcode & 0x0f00) >> 8];
            _pc += 2;
        }

        //0xFX1E : Adds VX to I. VF is not affected.
        private void CpuAddVXToI(UInt16 opcode){
            _I += _V[(opcode & 0x0f00) >> 8];
            _pc += 2;
        }

        //0xFX29 : Sets I to the location of the sprite for the character in VX. Characters 0-F (in hexadecimal) are represented by a 4x5 font.
        private void CpuSetIToSpriteLocationInVX(UInt16 opcode){
            _I = (UInt16)(_V[(opcode & 0x0f00) >> 8] * 5);
            _pc += 2;
        }

        //0xFX30 : Point I to 10-byte font sprite for digit VX (only digits 0-9)
        private void CpuSetITo10byteSpriteLocationInVX(UInt16 opcode){
            _I = (UInt16)((_V[(opcode & 0x0f00) >> 8] * 10) + _chip8_fontset.Length);
            _pc += 2;
        }

        //0xFX33 : Stores the binary-coded decimal representation of VX, with the most significant of three digits at the address in I, the middle digit at I plus 1, and the least significant digit at I plus 2. (In other words, take the decimal representation of VX, place the hundreds digit in memory at location in I, the tens digit at location I+1, and the ones digit at location I+2.);
        private void CpuStoreBCDOfVX(UInt16 opcode){
            Byte vx = _V[(opcode & 0x0f00) >> 8];
            _memory[_I] = (Byte)(vx / 100);
            _memory[_I + 1] = (Byte)(vx % 100 / 10);
            _memory[_I + 2] = (Byte)(vx % 100 % 10);
            _pc += 2;
        }

        //0xFX55 : Stores V0 to VX (including VX) in memory starting at address I. The offset from I is increased by 1 for each value written, but I itself is left unmodified.
        private void CpuStoreV0ToVXAtAddressI(UInt16 opcode){
            int x = (opcode & 0x0f00) >> 8;
            for(int i=0; i <= x; i++){
                _memory[_I + i] = _V[i];
            }
            if(mem_quirk){
                _I += (UInt16)(x + 1);
            }
            _pc += 2;       
        }

        //0xFX65 : Fills V0 to VX (including VX) with values from memory starting at address I. The offset from I is increased by 1 for each value written, but I itself is left unmodified.
        private void CpuFillV0ToVXWithValuesAtAddressI(UInt16 opcode){
            int x = (opcode & 0x0f00) >> 8;
            for(int i=0; i <= x; i++){
                _V[i] = _memory[_I + i];
            }
            if(mem_quirk){
                _I += (UInt16)(x + 1);
            }
            _pc += 2;
        }

        //0xFX75 : Store V0..VX in RPL user flags (X <= 7)
        private void CpuStoreV0ToVXInRPL(UInt16 opcode){
            int x = (opcode & 0x0f00) >> 8;
            for(int i=0; i <= x; i++){
                _userFlags[i] = _V[i];
            }
            _pc += 2;
        }

        //0xFX85 : Read V0..VX from RPL user flags (X <= 7)
        private void CpuReadV0ToVXFromRPL(UInt16 opcode){
            int x = (opcode & 0x0f00) >> 8;
            for(int i=0; i <= x; i++){
                _V[i] = _userFlags[i];
            }
            _pc += 2;
        }
    }
}