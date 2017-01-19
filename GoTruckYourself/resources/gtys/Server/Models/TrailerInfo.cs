using GTANetworkServer;
using GTANetworkShared;
using System.Linq;

namespace GoTruckYourself.Server.Models
{
    public class TrailerInfo
    {
        private const float CollisionShapeSize = 10;

        private Blip _blip;
        private CylinderColShape _destinationCollision;

        public Vehicle Vehicle { get; }

        public Vector3 Destination { get; }

        public bool IsOnDestination { get; private set; }

        public delegate void DeletedHandler(TrailerInfo trailerInfo);
        public event DeletedHandler Deleted;

        public delegate void EnteredDestinationHandler(TrailerInfo trailerInfo);
        public event EnteredDestinationHandler EnteredDestination;

        public delegate void DetachedOnDestinationHandler(TruckInfo truck, TrailerInfo trailerInfo);
        public event DetachedOnDestinationHandler DetachedOnDestination;

        private void ShowBlip(bool show)
        {
            if (!show && _blip == null) return;
            if (show && _blip != null) return;
            if (show && Vehicle == null || !Vehicle.exists) return;

            if (show)
            {
                _blip = API.shared.createBlip(Vehicle);
                _blip.sprite = 408;
                _blip.shortRange = true;
            }
            else
            {
                _blip.delete();
                _blip = null;
            }
        }

        public TrailerInfo(Vehicle vehicle, Vector3 destination)
        {
            Vehicle = vehicle;
            Destination = destination;

            _destinationCollision = API.shared.createCylinderColShape(
                destination,
                CollisionShapeSize,
                CollisionShapeSize);
            API.shared.consoleOutput("Create col shape: " + _destinationCollision.handle);

            _destinationCollision.onEntityEnterColShape += EntityEnteredDestination;
            _destinationCollision.onEntityExitColShape += EntityExitedDestination;
            
            ShowBlip(true);
        }

        private void EntityExitedDestination(ColShape shape, NetHandle entity)
        {
            if (entity != Vehicle || !Vehicle.exists || Vehicle.health <= 0) return;
            
            IsOnDestination = false;
        }

        private void EntityEnteredDestination(ColShape shape, NetHandle entity)
        {
            if (entity != Vehicle || !Vehicle.exists || Vehicle.health <= 0) return;
            
            IsOnDestination = true;
            EnteredDestination?.Invoke(this);
        }

        public void NotifyTraileredBy(TruckInfo truck)
        {
            ShowBlip(false);
        }

        public void NotifyTrailerDetached(TruckInfo truck)
        {
            if (IsOnDestination) DetachedOnDestination?.Invoke(truck, this);

            ShowBlip(true);
        }

        public void Delete()
        {
            API.shared.consoleOutput("Delete col shape: " + _destinationCollision.handle);
            API.shared.deleteColShape(_destinationCollision);
            ShowBlip(false);

            if (Vehicle.exists)
            {
                Vehicle.delete();
            }

            Deleted?.Invoke(this);
        }

        public Client GetTrailerDriver()
        {
            return Vehicle.traileredBy?.occupants.FirstOrDefault(o => o.vehicleSeat == -1);
        }
    }
}
