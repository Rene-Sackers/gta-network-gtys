using GTANetworkServer;
using GTANetworkShared;
using GoTruckYourself.Server.Extensions;
using GoTruckYourself.Server.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GoTruckYourself.Server
{
    public class Main : Script
    {
        private const float SpawnAreaClearanceRange = 15f;
        private const int UnhookedTrailerSpawnCount = 100;
        private const string ClientSetDestinationEventName = "SetDestination";

        private string _configPath { get { return API.getResourceFolder() + "\\Config\\Config.xml"; } }

        private Config _config;
        private Random _random = new Random();
        private List<TruckInfo> _trucks = new List<TruckInfo>();
        private List<TrailerInfo> _trailers = new List<TrailerInfo>();

        private VehicleHash[] _truckHashes = new[] {
            VehicleHash.Hauler,
            VehicleHash.Packer,
            VehicleHash.Phantom
        };

        private VehicleHash[] _trailerHashes = new[] {
            VehicleHash.ArmyTanker,
            VehicleHash.ArmyTrailer,
            VehicleHash.DockTrailer,
            VehicleHash.TR4,
            VehicleHash.TVTrailer,
            VehicleHash.Tanker,
            VehicleHash.Tanker2,
            VehicleHash.TrailerLogs,
            VehicleHash.Trailers,
            VehicleHash.Trailers2,
            VehicleHash.Trailers3
        };

        public Main()
        {
            API.onResourceStart += OnResourceStart;
            API.onResourceStop += OnResourceStop;
            API.onPlayerRespawn += OnPlayerRespawn;
            API.onPlayerConnected += OnPlayerConnected;
            API.onVehicleTrailerChange += OnVehicleTrailerChange;
            API.onPlayerEnterVehicle += OnPlayerEnterVehicle;
            API.onPlayerExitVehicle += OnPlayerExitVehicle;
        }

        private void OnPlayerConnected(Client player)
        {
            SpawnPlayerRandomly(player);
        }

        private TruckInfo GetTruckInfoByNetHandle(NetHandle netHandle)
        {
            return _trucks.FirstOrDefault(t => t.Vehicle == netHandle);
        }

        private TrailerInfo GetTrailerInfoByNetHandle(NetHandle netHandle)
        {
            return _trailers.FirstOrDefault(t => t.Vehicle == netHandle);
        }

        private void TogglePlayerDestinationBlip(Client player, TruckInfo truck, bool showBlip)
        {
            API.triggerClientEvent(player, ClientSetDestinationEventName, showBlip ? truck.Trailer.Destination : null);
        }

        private void OnPlayerEnterVehicle(Client player, NetHandle vehicle)
        {
            var truckInfo = GetTruckInfoByNetHandle(vehicle);
            if (truckInfo == null)
            {
                API.sendNotificationToPlayer(player, "This is an unknown truck!");
                return;
            }

            if (truckInfo.Trailer == null) return;

            TogglePlayerDestinationBlip(player, truckInfo, true);
        }

        private void OnPlayerExitVehicle(Client player, NetHandle vehicle)
        {
            var truckInfo = GetTruckInfoByNetHandle(vehicle);
            if (truckInfo == null || truckInfo.Trailer == null) return;

            TogglePlayerDestinationBlip(player, truckInfo, false);
        }

        private void OnVehicleTrailerChange(NetHandle tower, NetHandle trailer)
        {
            Log("Trailer change. Tower: " + tower + ", trailer: " + trailer);

            var truckInfo = GetTruckInfoByNetHandle(tower);
            if (truckInfo == null) return;

            var player = truckInfo.GetDriver();
            var trailerInfo = GetTrailerInfoByNetHandle(trailer);
            
            // Truck detached from current trailer
            if (trailerInfo == null && truckInfo.Trailer != null)
            {
                truckInfo.Trailer.NotifyTrailerDetached(truckInfo);
                truckInfo.Trailer = null;
                TogglePlayerDestinationBlip(player, truckInfo, false);
                return;
            }
            
            // Attached to unknown trailer
            if (trailerInfo == null)
            {
                var driver = API.getVehicleOccupants(tower).FirstOrDefault(o => o.vehicleSeat == -1);
                if (driver == null) return;

                API.sendNotificationToPlayer(driver, "This is an unknown trailer!");
                return;
            }
            
            truckInfo.Trailer = trailerInfo;
            trailerInfo.NotifyTraileredBy(truckInfo);

            TogglePlayerDestinationBlip(player, truckInfo, true);
        }

        private void OnPlayerRespawn(Client player)
        {
            SpawnPlayerRandomly(player);
        }

        private void OnResourceStart()
        {
            _config = Config.LoadConfig(_configPath);
            ResetGame();
        }

        private void OnResourceStop()
        {
            DeleteVehicles();
        }

        private void DeleteVehicles()
        {
            for (var i = _trucks.Count - 1; i >= 0; i--)
                _trucks[i].Delete();

            for (var i = _trailers.Count - 1; i >= 0; i--)
                _trailers[i].Delete();
        }

        private void ResetGame()
        {
            DeleteVehicles();

            SpawnTrailers();
            API.getAllPlayers().ForEach(SpawnPlayerRandomly);
        }

        private void SpawnTrailers()
        {
            var unhookedTrailerCount = _trailers.Count(t => t.Vehicle.traileredBy == null);
            
            for (var i = unhookedTrailerCount; i < UnhookedTrailerSpawnCount; i++)
            {
                SpawnTrailerRandomly();
            }
        }

        private void SpawnTrailerRandomly()
        {
            var spawnPoint = FindClearSpawnPoint(SpawnPointTypes.Trailer);
            if (spawnPoint == null)
            {
                Log("No clear spawnpoints for trailers!");
                return;
            }

            var targetPoint = _config.SpawnPoints.Shuffle().FirstOrDefault(sp => sp.Type == SpawnPointTypes.Destination);
            if (targetPoint == null)
            {
                Log("No destinations!");
                return;
            }

            var trailerHash = _trailerHashes[_random.Next(0, _trailerHashes.Length)];

            var trailerInfo = new TrailerInfo(API.createVehicle(trailerHash, spawnPoint.Position, spawnPoint.Rotation, 0, 0), targetPoint.Position);
            trailerInfo.Deleted += TrailerDeleted;
            trailerInfo.EnteredDestination += TrailerEnteredDestination;
            trailerInfo.DetachedOnDestination += TrailerDetachedOnDestination;

            _trailers.Add(trailerInfo);
        }

        private void TrailerDeleted(TrailerInfo trailerInfo)
        {
            if (!_trailers.Contains(trailerInfo)) return;
            _trailers.Remove(trailerInfo);
        }

        private void TrailerEnteredDestination(TrailerInfo trailerInfo)
        {
            var driver = trailerInfo.GetTrailerDriver();
            if (driver == null) return;

            API.sendNotificationToPlayer(driver, "Detach the trailer to finish delivery!");
        }

        private void TrailerDetachedOnDestination(TruckInfo truckInfo, TrailerInfo trailerInfo)
        {
            var driver = truckInfo.GetDriver();
            if (driver == null) return;

            API.sendNotificationToPlayer(driver, "Well done!");
            trailerInfo.Delete();

            SpawnTrailers();
        }

        private bool AreVehiclesInRange(Vector3 position, float range)
        {
            return API.getAllVehicles().Any(v => API.getEntityPosition(v).DistanceTo(position) <= range);
        }

        private SpawnPoint FindClearSpawnPoint(SpawnPointTypes type)
        {
            return _config
                .SpawnPoints
                .Shuffle()
                .FirstOrDefault(sp => sp.Type == type && !AreVehiclesInRange(sp.Position, SpawnAreaClearanceRange));
        }

        private void SpawnPlayerRandomly(Client player)
        {
            var spawnPoint = FindClearSpawnPoint(SpawnPointTypes.Truck);
            if (spawnPoint == null)
            {
                player.sendChatMessage("No clear spawnpoints for trucks!");
                return;
            }

            var truck = CreateTruck(spawnPoint.Position, spawnPoint.Rotation);
            player.setIntoVehicle(truck.Vehicle, -1);
        }

        private TruckInfo CreateTruck(Vector3 position, Vector3 rotation)
        {
            var truckHash = _truckHashes[_random.Next(0, _truckHashes.Length)];
            var truck = new TruckInfo(API.createVehicle(truckHash, position, rotation, 0, 0));
            truck.Deleted += TruckDeleted;

            _trucks.Add(truck);

            return truck;
        }

        private void TruckDeleted(TruckInfo truckInfo)
        {
            _trucks.Remove(truckInfo);
        }

        public static void Log(string message)
        {
            API.shared.consoleOutput("GoTruckYourself: " + message);
        }

        [Command(Alias = "dts")]
        public void DeleteTruckSpawnCommand(Client client, int type = (int)SpawnPointTypes.Truck)
        {
            var closestPoint = _config.SpawnPoints
                .Where(sp => sp.Position.DistanceTo(client.position) < 10)
                .OrderBy(sp => sp.Position.DistanceTo(client.position))
                .FirstOrDefault();

            if (closestPoint == null) return;

            _config.SpawnPoints.Remove(closestPoint);
            _config.Save(_configPath);

            API.sendChatMessageToPlayer(client, "Deleted spawn point.");
        }


        [Command(Alias = "cts", ACLRequired = true, Group = "Admin")]
        public void CreateTruckSpawnCommand(Client client, int type = (int)SpawnPointTypes.Truck)
        {
            var rotation = new Vector3(0, 0, client.rotation.Z);
            _config.SpawnPoints.Add(new SpawnPoint { Position = client.position, Rotation = rotation, Type = (SpawnPointTypes)type });
            _config.Save(_configPath);

            API.sendChatMessageToPlayer(client, "Created spawn point. " + (SpawnPointTypes)type);
        }


        [Command(Alias = "res", ACLRequired = true, Group = "Admin")]
        public void RestartCommand(Client client)
        {
            ResetGame();
        }

        private List<NetHandle> _markers = new List<NetHandle>();
        private bool _showingMarkers = false;

        [Command(Alias ="tmarkers", ACLRequired = true)]
        public void ShowMarkersCommand(Client client)
        {
            if (_showingMarkers)
            {
                foreach (var marker in _markers)
                {
                    API.deleteEntity(marker);
                }
                _markers.Clear();

                _showingMarkers = false;
                return;
            }

            foreach (var spawnPoint in _config.SpawnPoints)
            {
                var marker = API.createMarker((int)spawnPoint.Type, spawnPoint.Position, new Vector3(), new Vector3(), new Vector3(1, 1, 1), 255, 255, 0, 0);
                _markers.Add(marker);

                var blip = API.createBlip(marker);
                blip.sprite = 64;
                blip.color = (int)spawnPoint.Type + 1;
                _markers.Add(blip);
            }

            _showingMarkers = true;
        }
    }
}
