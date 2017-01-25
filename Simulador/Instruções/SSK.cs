using System;
using System.Text.RegularExpressions;

namespace Simulador
{
    class SSK : SetInstrucoes
    {
        public SSK()
        {
            base.tamanho = 3;
            base.codigo = 0xEC;
        }

        override public void Execute(ULA ula)
        {
            try
            {
                ula.memoria.WriteKey((int)ula.parametroCarregado, ula.registradores[ula.A]);
            }
            catch (Exception)
            {                
                throw;
            }
        }

        //override public string OPCode(SIC sic)
        //{
        //    int codInst = 0xEC;

        //    return base.OPCodeINST3(sic, codInst);
        //}

        public override string ToString()
        {
            return "SSK";
        }
    }
}