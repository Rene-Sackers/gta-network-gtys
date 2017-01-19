using GTANetworkServer;
using System.Linq;

namespace GoTruckYourself.Server.Models
{
    public class TruckInfo
    {
        public Vehicle Vehicle { get; }

        public TrailerInfo Trailer { get; set; }

        public delegate void DeletedHandler(TruckInfo truckInfo);
        public event DeletedHandler Deleted;

        public TruckInfo(Vehicle vehicle)
        {
            Vehicle = vehicle;
        }

        public void Delete()
        {
            if (!Vehicle.exists) return;

            // Remove occupants
            foreach (var occupant in Vehicle.occupants)
            {
                occupant.warpOutOfVehicle(Vehicle);
            }
            
            if (Vehicle.exists)
            {
                Vehicle.delete();
            }

            Deleted?.Invoke(this);
        }


        public Client GetDriver()
        {
            return Vehicle.occupants.FirstOrDefault(o => o.vehicleSeat == -1);
        }
    }
}
