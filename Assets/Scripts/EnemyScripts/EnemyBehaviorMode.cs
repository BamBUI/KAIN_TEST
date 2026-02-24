namespace Assets.Scripts.EnemyScripts
{
    /// <summary>
    /// Режимы поведения врага
    /// </summary>
    public enum EnemyBehaviorMode
    {
        Guard,    // Ждать на спауне, гнаться если видит, вернуться на спаун
        Patrol   // Ходить по точкам, гнаться если видит, вернуться на патруль
    }
}