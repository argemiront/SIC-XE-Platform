﻿using System;
using System.Windows;
using System.Linq;
using System.Text.RegularExpressions;

namespace Simulador
{
    class LDL : SetInstrucoes
    {
        public LDL()
        {
            base.tamanho = 3;
            base.codigo = 0x08;
        }

        override public void Execute(ULA ula)
        {
            try
            {
                ula.registradores[ula.L] = ula.parametroCarregado;
            }
            catch (Exception ex)
            {
            }
        }

        //override public string OPCode(SIC sic)
        //{
        //    int codInst = 0x08;

        //    return base.OPCodeINST3(sic, codInst);
        //}

        public override string ToString()
        {
            return "LDL";
        }
    }
}