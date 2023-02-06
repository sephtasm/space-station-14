using Content.Server.Climbing;
using Content.Server.Power.Components;
using Content.Shared.Destructible;
using Content.Shared.ActionBlocker;
using Content.Shared.DragDrop;
using Content.Shared.Movement.Events;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Content.Server.MachineLinking.System;
using Content.Server.MachineLinking.Events;
using Content.Server.Cloning.Components;
using Content.Server.Construction;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Server.Containers;
using Content.Server.Genetics.GenePod;
using static Content.Shared.Genetics.GenePod.SharedGenePodComponent;
using Content.Server.Genetics.Components;
using Content.Server.Storage.Components;
using Content.Shared.Atmos.Piping.Binary.Components;

namespace Content.Server.Genetics
{
    public sealed class GenePodSystem : EntitySystem
    {
        [Dependency] private readonly SignalLinkerSystem _signalSystem = default!;
        [Dependency] private readonly ActionBlockerSystem _blocker = default!;
        [Dependency] private readonly ClimbSystem _climbSystem = default!;
        [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
        [Dependency] private readonly ContainerSystem _containerSystem = default!;
        [Dependency] private readonly GeneticsConsoleSystem _consoleSystem = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;

        private const float UpdateRate = 1f;
        private float _updateDif;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GenePodComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<GenePodComponent, ContainerRelayMovementEntityEvent>(OnRelayMovement);
            SubscribeLocalEvent<GenePodComponent, GetVerbsEvent<InteractionVerb>>(AddInsertOtherVerb);
            SubscribeLocalEvent<GenePodComponent, GetVerbsEvent<AlternativeVerb>>(AddAlternativeVerbs);
            SubscribeLocalEvent<GenePodComponent, DestructionEventArgs>(OnDestroyed);
            SubscribeLocalEvent<GenePodComponent, DragDropEvent>(HandleDragDropOn);
            SubscribeLocalEvent<GenePodComponent, PortDisconnectedEvent>(OnPortDisconnected);
            SubscribeLocalEvent<GenePodComponent, AnchorStateChangedEvent>(OnAnchorChanged);
            SubscribeLocalEvent<GenePodComponent, RefreshPartsEvent>(OnRefreshParts);
            SubscribeLocalEvent<GenePodComponent, UpgradeExamineEvent>(OnUpgradeExamine);
        }

        private void OnComponentInit(EntityUid uid, GenePodComponent scannerComponent, ComponentInit args)
        {
            base.Initialize();
            scannerComponent.BodyContainer = _containerSystem.EnsureContainer<ContainerSlot>(uid, $"genePod-bodyContainer");
            _signalSystem.EnsureReceiverPorts(uid, GenePodComponent.PodPort);
        }

        private void OnRelayMovement(EntityUid uid, GenePodComponent scannerComponent, ref ContainerRelayMovementEntityEvent args)
        {
            if (!_blocker.CanInteract(args.Entity, scannerComponent.Owner))
                return;

            EjectBody(uid, scannerComponent);
        }

        private void AddInsertOtherVerb(EntityUid uid, GenePodComponent component, GetVerbsEvent<InteractionVerb> args)
        {
            if (args.Using == null ||
                !args.CanAccess ||
                !args.CanInteract ||
                IsOccupied(component) ||
                !component.CanInsert(args.Using.Value))
                return;

            string name = "Unknown";
            if (TryComp<MetaDataComponent>(args.Using.Value, out var metadata))
                name = metadata.EntityName;

            InteractionVerb verb = new()
            {
                Act = () => InsertBody(component.Owner, args.Target, component),
                Category = VerbCategory.Insert,
                Text = name
            };
            args.Verbs.Add(verb);
        }

        private void AddAlternativeVerbs(EntityUid uid, GenePodComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanAccess || !args.CanInteract)
                return;

            // Eject verb
            if (IsOccupied(component))
            {
                AlternativeVerb verb = new();
                verb.Act = () => EjectBody(uid, component);
                verb.Category = VerbCategory.Eject;
                verb.Text = Loc.GetString("gene-pod-verb-noun-occupant");
                verb.Priority = 1; // Promote to top to make ejecting the ALT-click action
                args.Verbs.Add(verb);
            }

            // Self-insert verb
            if (!IsOccupied(component) &&
                component.CanInsert(args.User) &&
                _blocker.CanMove(args.User))
            {
                AlternativeVerb verb = new();
                verb.Act = () => InsertBody(component.Owner, args.User, component);
                verb.Text = Loc.GetString("gene-pod-verb-enter");
                args.Verbs.Add(verb);
            }
        }

        private void OnDestroyed(EntityUid uid, GenePodComponent scannerComponent, DestructionEventArgs args)
        {
            EjectBody(uid, scannerComponent);
        }

        private void HandleDragDropOn(EntityUid uid, GenePodComponent scannerComponent, DragDropEvent args)
        {
            // TODO: this action succeeds before the action bad fills up, removing the opportunity for the target to resist. medical scanner also does this
            InsertBody(uid, args.Dragged, scannerComponent);
        }

        private void OnPortDisconnected(EntityUid uid, GenePodComponent component, PortDisconnectedEvent args)
        {
            component.ConnectedConsole = null;
        }

        private void OnAnchorChanged(EntityUid uid, GenePodComponent component, ref AnchorStateChangedEvent args)
        {
            if (component.ConnectedConsole == null || !TryComp<GeneticsConsoleComponent>(component.ConnectedConsole, out var console))
                return;

            if (args.Anchored)
            {
                _consoleSystem.RecheckConnections((EntityUid) component.ConnectedConsole, uid, console);
                return;
            }
            //_cloningConsoleSystem.UpdateUserInterface(console);
        }
        private GenePodStatus GetStatus(GenePodComponent scannerComponent)
        {
            if (TryComp<ApcPowerReceiverComponent>(scannerComponent.Owner, out var power) && power.Powered)
            {
                var body = scannerComponent.BodyContainer.ContainedEntity;
                if (body == null)
                    return GenePodStatus.Unoccupied;

                if (!TryComp<MobStateComponent>(body.Value, out var state))
                {
                    return GenePodStatus.Unoccupied;
                }

                return GetStatusFromDamageState(body.Value, state);
            }
            return GenePodStatus.Off;
        }

        public bool IsOccupied(GenePodComponent scannerComponent)
        {
            return scannerComponent.BodyContainer.ContainedEntity != null;
        }

        private GenePodStatus GetStatusFromDamageState(EntityUid uid, MobStateComponent state)
        {
            if (_mobStateSystem.IsAlive(uid, state))
                return GenePodStatus.Occupied;

            if (_mobStateSystem.IsCritical(uid, state))
                return GenePodStatus.Occupied;

            if (_mobStateSystem.IsDead(uid, state))
                return GenePodStatus.Occupied;

            return GenePodStatus.Unoccupied;
        }

        private void UpdateAppearance(EntityUid uid, GenePodComponent component)
        {
            if (TryComp<AppearanceComponent>(component.Owner, out var appearance))
            {
                _appearanceSystem.SetData(uid, GenePodVisuals.Status, GetStatus(component));
            }
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            _updateDif += frameTime;
            if (_updateDif < UpdateRate)
                return;

            _updateDif -= UpdateRate;

            foreach (var scanner in EntityQuery<GenePodComponent>())
            {
                UpdateAppearance(scanner.Owner, scanner);
            }
        }

        public void InsertBody(EntityUid uid, EntityUid user, GenePodComponent? component)
        {
            if (!Resolve(uid, ref component))
                return;

            if (component.BodyContainer.ContainedEntity != null)
                return;

            if (!TryComp<MobStateComponent>(user, out var comp))
                return;

            component.BodyContainer.Insert(user);
            UpdateAppearance(component.Owner, component);
        }

        public void EjectBody(EntityUid uid, GenePodComponent? component)
        {
            if (!Resolve(uid, ref component))
                return;

            if (component.BodyContainer.ContainedEntity is not {Valid: true} contained) return;

            component.BodyContainer.Remove(contained);
            component.LastScannedBody = null;
            component.Scanning = false;
            _climbSystem.ForciblySetClimbing(contained, uid);
            UpdateAppearance(component.Owner, component);
        }

        private void OnRefreshParts(EntityUid uid, GenePodComponent component, RefreshPartsEvent args)
        {
            var ratingDamageReduction = args.PartRatings[component.MachinePartDamageReduction];

            component.DamageReductionMultiplier = MathF.Pow(component.PartRatingDamageReductionMultiplier, ratingDamageReduction - 1);
        }

        private void OnUpgradeExamine(EntityUid uid, GenePodComponent component, UpgradeExamineEvent args)
        {
            args.AddPercentageUpgrade("gene-pod-upgrade-damage-reduction", component.DamageReductionMultiplier);
        }
    }
}