using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

// Nodo de Unity Behavior para el ataque del enemigo
[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Atacar jugador", story: "Atacar jugador con [Agent]", category: "Action/Mazmorra", id: "7f48a159b1b8437f97f991ab128ac001")]
public partial class AtacarEnemigo : Action
{
    #region CLASS_VARIABLES

    [SerializeReference] public BlackboardVariable<GameObject> Agent;

    #endregion

    #region CLASS_METHODS

    protected override Status OnStart() => Evaluar();
    protected override Status OnUpdate() => Evaluar();

    private Status Evaluar()
    {
        Enemy enemigo = Agent?.Value != null ? Agent.Value.GetComponent<Enemy>() : null;
        if (enemigo == null || !enemigo.PuedeActuar)
            return Status.Failure;

        enemigo.NotificarNodo();
        return enemigo.IntentarAtacar() ? Status.Success : Status.Failure;
    }

    #endregion
}
