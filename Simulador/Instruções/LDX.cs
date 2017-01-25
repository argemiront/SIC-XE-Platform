using System;
using System.Windows;
using System.Linq;
using System.Text.RegularExpressions;

namespace Simulador
{
    class LDX : SetInstrucoes
    {
        public LDX()
        {
            base.tamanho = 3;
            base.codigo = 0x04;
        }

        override public void Execute(ULA ula)
        {
            try
            {
                ula.registradores[ula.X] = ula.parametroCarregado;
            }
            catch (Exception ex)
            {
            }
        }

        //override public string OPCode(SIC sic)
        //{
        //    int codInst = 0x04;

        //    return base.OPCodeINST3(sic, codInst);
        //}

        public override string ToString()
        {
            return "LDX";
        }
    }
}