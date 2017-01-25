using System;
using System.Windows;
using System.Linq;
using System.Text.RegularExpressions;

namespace Simulador
{
    class WD : SetInstrucoes
    {
        public WD()
        {
            base.tamanho = 3;
            base.codigo = 0xDC;
        }

        override public void Execute(ULA ula)
        {
            try
            {
                ula.dispES.WriteIO((byte)ula.parametroCarregado, (byte)(ula.registradores[ula.A] & 0xFF));
            }
            catch (Exception ex)
            {
            }
        }

        //override public string OPCode(SIC sic)
        //{
        //    int codInst = 0xDC;

        //    return base.OPCodeINST3(sic, codInst);
        //}

        public override string ToString()
        {
            return "WD";
        }
    }
}