using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

// Nodo de Unity Behavior para la patrulla o espera de un enemigo
[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Patrullar o esperar", story: "Patrullar o esperar con [Agent]", category: "Action/Mazmorra", id: "7f48a159b1b8437f97f991ab128ac003")]
public partial class PatrullarEnemigo : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;

    protected override Status OnStart() => Evaluar();
    protected override Status OnUpdate() => Evaluar();

    private Status Evaluar()
    {
        Enemy enemigo = Agent?.Value != null ? Agent.Value.GetComponent<Enemy>() : null;
        if (enemigo == null || !enemigo.PuedeActuar) return Status.Failure;
        enemigo.NotificarNodo();
        return enemigo.Patrullar() ? Status.Success : Status.Failure;
    }
}
