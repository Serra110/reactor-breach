using UnityEngine;

namespace SimpleBuildingSystem
{
   
    public class BuildingSocket : MonoBehaviour
    {
        [Tooltip("Tipos de part (partId) que este socket aceita. Vazio = aceita qualquer uma.")]
        public string[] allowedPartIds;

        [Tooltip("Distância máxima a que uma peça em preview começa a fazer snap a este socket.")]
        public float snapRadius = 0.5f;

        // FIX: um socket só pode ter uma peça ligada de cada vez. Sem isto, duas peças podiam
        // tentar ocupar o mesmo ponto e ficavam sobrepostas, ou o segundo confirm "roubava"
        // o lugar do primeiro, deixando um buraco onde a peça devia ficar.
        public bool IsOccupied { get; private set; }
        public BuildingPart ConnectedPart { get; private set; }

        public bool AcceptsPart(BuildingPart part)
        {
            if (IsOccupied) return false;
            if (part == null) return true;
            if (allowedPartIds == null || allowedPartIds.Length == 0) return true;

            foreach (var id in allowedPartIds)
                if (id == part.partId) return true;

            return false;
        }

        public Vector3 GetSnapPosition() => transform.position;
        public Quaternion GetSnapRotation() => transform.rotation;

        public void Connect(BuildingPart part)
        {
            IsOccupied = true;
            ConnectedPart = part;
        }

        public void Disconnect()
        {
            IsOccupied = false;
            ConnectedPart = null;
        }

        // Procura o socket livre mais próximo dentro de um raio, filtrando por tipo de Part.
        public static BuildingSocket FindNearest(Vector3 worldPos, float searchRadius, BuildingPart forPart)
        {
            BuildingSocket best = null;
            float bestDist = float.MaxValue;

            var colliders = Physics.OverlapSphere(worldPos, searchRadius);
            foreach (var col in colliders)
            {
                var socket = col.GetComponent<BuildingSocket>();
                if (socket == null) continue;

                // Ignora sockets que pertencem à própria peça em preview (evita auto-snap/jitter)
                if (forPart != null)
                {
                    var rootPart = socket.GetComponentInParent<BuildingPart>();
                    if (rootPart != null && rootPart == forPart)
                        continue;
                }

                // AcceptsPart já rejeita sockets ocupados
                if (!socket.AcceptsPart(forPart)) continue;

                float dist = Vector3.Distance(worldPos, socket.transform.position);
                if (dist < socket.snapRadius && dist < bestDist)
                {
                    bestDist = dist;
                    best = socket;
                }
            }

            return best;
        }
    }
}