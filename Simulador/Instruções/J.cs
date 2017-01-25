using System;
using System.Windows;
using System.Linq;
using System.Text.RegularExpressions;

namespace Simulador
{
    class J : SetInstrucoes
    {
        public J()
        {
            base.tamanho = 3;
            base.codigo = 0x3C;
        }

        override public void Execute(ULA ula)
        {
            try
            {
                ula.registradores[ula.PC] = ula.enderecoCarregado;          
            }
            catch (Exception ex)
            {
            }
        }

        //override public string OPCode(SIC sic)
        //{
        //    int codInst = 0x3C;

        //    return base.OPCodeINST3(sic, codInst);
        //}

        public override string ToString()
        {
            return "J";
        }
    }
}