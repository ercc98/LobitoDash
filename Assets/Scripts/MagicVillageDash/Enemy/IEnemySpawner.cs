using System;
using MagicVillageDash.Enemy;

public interface IEnemySpawner
{
    void Spawn();
    event Action<EnemyController> OnSpawnedEnemy;
    event Action<int> OnStartSpawnEnemy;
}