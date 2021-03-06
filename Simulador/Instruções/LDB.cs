﻿using System;
using System.Windows;
using System.Linq;
using System.Text.RegularExpressions;

namespace Simulador
{
    class LDB : SetInstrucoes
    {
        public LDB()
        {
            base.tamanho = 3;
            base.codigo = 0x68;
        }

        override public void Execute(ULA ula)
        {
            try
            {
                ula.registradores[ula.B] = ula.parametroCarregado;
            }
            catch (Exception ex)
            {
            }
        }

        //override public string OPCode(SIC sic)
        //{
        //    int codInst = 0x68;

        //    return base.OPCodeINST3(sic, codInst);
        //}

        public override string ToString()
        {
            return "LDB";
        }
    }
}