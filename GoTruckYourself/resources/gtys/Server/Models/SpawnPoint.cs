using GTANetworkShared;

namespace GoTruckYourself.Server.Models
{
    public class SpawnPoint
    {
        public SpawnPointTypes Type { get; set; }

        public Vector3 Position { get; set; }

        public Vector3 Rotation { get; set; }
    }
}
