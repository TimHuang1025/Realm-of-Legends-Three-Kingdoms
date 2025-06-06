using System;

namespace Game.Core   // 如果你有命名空间，可换成自己的
{
    [Serializable]
    public struct Stats4
    {
        public int Atk;
        public int Def;
        public int Int;
        public int Cmd;

        public Stats4(int atk, int def, int intel, int cmd)
        {
            Atk = atk; Def = def; Int = intel; Cmd = cmd;
        }

        // ★ 允许 (atk,def,int,cmd) = stats
        public void Deconstruct(out int atk, out int def,
                                out int intel, out int cmd)
        {
            atk = Atk; def = Def; intel = Int; cmd = Cmd;
        }

        public static Stats4 operator +(Stats4 a, Stats4 b) =>
            new Stats4(a.Atk + b.Atk,
                    a.Def + b.Def,
                    a.Int + b.Int,
                    a.Cmd + b.Cmd);
    }

}
