using EloBuddy;
using EloBuddy.SDK;

namespace ApZilean
{
    static class DamageIndicator
    {
        private static float Qdamage(Obj_AI_Base target)
        {
            if (Program.Q.IsReady())
            {
                return Player.Instance.CalculateDamageOnUnit(target, DamageType.Magical, (float)(new float[] { 0, 75, 115, 165, 230, 300 }[Program.Q.Level] + 0.9f * Player.Instance.TotalMagicalDamage));
            }
            else return 0f;
        }

        public static bool HasQZileanBuff(this Obj_AI_Base target)
        {
            if (target.IsEnemy)
            {
                return target.HasBuff("ZileanQEnemyBomb");
            }
            else
            {
                return target.HasBuff("ZileanQAllyBomb");
            }
        }

        public static float Damagefromspell(Obj_AI_Base target)
        {
            if (target == null)
            {
                return 0f;
            }
            else
            {
                var QDamage = Program.Q.GetSpellDamage(target);

                if (Program.Q.IsReady() && Program.W.IsReady()) return QDamage * 2;

                else if (Program.Q.IsReady() && !Program.W.IsReady()) return QDamage;
                
                else if (!Program.Q.IsReady() && Program.W.IsReady()) return QDamage;
                
                else return 0f;
            }
        }
    }
}
