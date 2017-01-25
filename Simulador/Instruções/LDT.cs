using System;
using System.Windows;
using System.Linq;
using System.Text.RegularExpressions;

namespace Simulador
{
    class LDT : SetInstrucoes
    {
        public LDT()
        {
            base.tamanho = 3;
            base.codigo = 0x74;
        }

        override public void Execute(ULA ula)
        {
            try
            {
                ula.registradores[ula.T] = ula.parametroCarregado;
            }
            catch (Exception ex)
            {
            }
        }

        //override public string OPCode(SIC sic)
        //{
        //    int codInst = 0x74;

        //    return base.OPCodeINST3(sic, codInst);
        //}

        public override string ToString()
        {
            return "LDT";
        }
    }
}