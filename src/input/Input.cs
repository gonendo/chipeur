using System;
using Veldrid;
using chipeur.gui;

namespace chipeur.input
{
    class Input{
        public const int KEYBOARD_LAYOUT_AZERTY = 1;
        public const int KEYBOARD_LAYOUT_QWERTY = 2;
        public static int keyboardLayout = KEYBOARD_LAYOUT_QWERTY;
        public Byte[] key {get; set;}
        private Key[] keymap = new Key[16]{
            Key.X,
            Key.Number1,
            Key.Number2,
            Key.Number3,
            Key.A,
            Key.Z,
            Key.E,
            Key.Q,
            Key.S,
            Key.D,
            Key.W,
            Key.C,
            Key.Number4,
            Key.R,
            Key.F,
            Key.V
        };
        public Input(){
            Gui.KeyDown += (Key k) =>
            {
                for(int i=0; i < keymap.Length; i++){
                    if(k == keymap[i]){
                        key[i] = 1;
                    }
                }
            };
            Gui.KeyUp += (Key k) =>
            {
                for(int i=0; i < keymap.Length; i++){
                    if(k == keymap[i]){
                        key[i] = 0;
                    }
                }
            };
            Gui.ChangeKeyboardLayout += (int layout) =>
            {
                if(layout == KEYBOARD_LAYOUT_AZERTY){
                    keymap[4] = Key.Q;
                    keymap[5] = Key.W;
                    keymap[7] = Key.A;
                    keymap[10] = Key.Z;
                }
                else if(layout == KEYBOARD_LAYOUT_QWERTY){
                    keymap[4] = Key.A;
                    keymap[5] = Key.Z;
                    keymap[7] = Key.Q;
                    keymap[10] = Key.W;
                }
                Input.keyboardLayout = layout;
            };
        }

        public void Initialize(){
            key = new Byte[16];
        }
    }
}