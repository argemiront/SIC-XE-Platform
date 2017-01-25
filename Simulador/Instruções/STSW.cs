using System;
using System.Windows;
using System.Linq;
using System.Text.RegularExpressions;

namespace Simulador
{
    class STSW : SetInstrucoes
    {
        public STSW()
        {
            base.tamanho = 3;
            base.codigo = 0xE8;
        }

        override public void Execute(ULA ula)
        {
            try
            {
                ula.memoria.Write(ula.enderecoCarregado, 3, ula.registradores[ula.SW] & 0xFFFFFF);
            }
            catch (Exception ex)
            {
            }
        }

        //override public string OPCode(SIC sic)
        //{
        //    int codInst = 0xE8;

        //    return base.OPCodeINST3(sic, codInst);
        //}

        public override string ToString()
        {
            return "STSW";
        }
    }
}