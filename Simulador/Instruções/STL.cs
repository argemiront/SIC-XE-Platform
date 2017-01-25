using System;
using System.Windows;
using System.Linq;
using System.Text.RegularExpressions;

namespace Simulador
{
    class STL : SetInstrucoes
    {
        public STL()
        {
            base.tamanho = 3;
            base.codigo = 0x14;
        }

        override public void Execute(ULA ula)
        {
            try
            {
                ula.memoria.Write(ula.enderecoCarregado, 3, ula.registradores[ula.L] & 0xFFFFFF);
            }
            catch (Exception ex)
            {
            }
        }

        //override public string OPCode(SIC sic)
        //{
        //    int codInst = 0x14;

        //    return base.OPCodeINST3(sic, codInst);
        //}

        public override string ToString()
        {
            return "STL";
        }
    }
}