using System;
using System.Windows;
using System.Linq;
using System.Text.RegularExpressions;

namespace Simulador
{
    class STS : SetInstrucoes
    {
        public STS()
        {
            base.tamanho = 3;
            base.codigo = 0x7C;
        }

        override public void Execute(ULA ula)
        {
            try
            {
                ula.memoria.Write(ula.enderecoCarregado, 3, ula.registradores[ula.S] & 0xFFFFFF);
            }
            catch (Exception ex)
            {
            }
        }

        //override public string OPCode(SIC sic)
        //{
        //    int codInst = 0x7C;

        //    return base.OPCodeINST3(sic, codInst);
        //}

        public override string ToString()
        {
            return "STS";
        }
    }
}