using System;
using System.Windows;
using System.Linq;
using System.Text.RegularExpressions;

namespace Simulador
{
    class STF : SetInstrucoes
    {
        public STF()
        {
            base.tamanho = 3;
            base.codigo = 0x80;
        }

        override public void Execute(ULA ula)
        {
            try
            {
                ula.memoria.Write(ula.enderecoCarregado, 6, ula.registradores[ula.F] & 0xFFFFFFFFFFFF);
            }
            catch (Exception ex)
            {
            }
        }

        //override public string OPCode(SIC sic)
        //{
        //    int codInst = 0x78;

        //    return base.OPCodeINST3(sic, codInst);
        //}

        public override string ToString()
        {
            return "STF";
        }
    }
}