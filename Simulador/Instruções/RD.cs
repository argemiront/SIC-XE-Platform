using System;
using System.Windows;
using System.Linq;
using System.Text.RegularExpressions;

namespace Simulador
{
    class RD : SetInstrucoes
    {
        public RD()
        {
            base.tamanho = 3;
            base.codigo = 0xD8;
        }

        override public void Execute(ULA ula)
        {
            try
            {
                byte valorDisp = ula.dispES.ReadIO((byte)ula.parametroCarregado);
                ula.registradores[ula.A] = (ula.registradores[ula.A] & 0xFFFF00) + valorDisp;
            }
            catch (Exception ex)
            {
            }
        }

        //override public string OPCode(SIC sic)
        //{
        //    int codInst = 0xD8;

        //    return base.OPCodeINST3(sic, codInst);
        //}

        public override string ToString()
        {
            return "RD";
        }
    }
}