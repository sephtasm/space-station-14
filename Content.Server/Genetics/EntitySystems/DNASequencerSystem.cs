using Content.Server.DoAfter;
using Content.Shared.Database;
using Content.Shared.Hands;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Robust.Shared.GameStates;
using System.Threading;
using Content.Shared.Mobs.Components;
using Content.Server.Popups;
using Content.Server.Genetics.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.CombatMode;
using Content.Shared.Mobs.Systems;
using Content.Server.Administration.Logs;
using Content.Shared.Genetics;

namespace Content.Server.Genetics.EntitySystems;

public sealed class DNASequencerSystem : EntitySystem
{

    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly GeneticsSystem _genetics = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedCombatModeSystem _combat = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<DNASequencerComponent, DNASequenceChangedEvent>(OnDNASequenceChange);
        SubscribeLocalEvent<DNASequencerComponent, HandDeselectedEvent>(OnDNASequencerDeselected);
        SubscribeLocalEvent<DNASequencerComponent, ComponentStartup>(OnDNASequencerStartup);
        SubscribeLocalEvent<DNASequencerComponent, UseInHandEvent>(OnDNASequencerUse);
        SubscribeLocalEvent<DNASequencerComponent, AfterInteractEvent>(OnDNASequencerAfterInteract);
        SubscribeLocalEvent<DNASequencerComponent, ComponentGetState>(OnDNASequencerGetState);

        SubscribeLocalEvent<ModificationCompleteEvent>(OnModificationComplete);
        SubscribeLocalEvent<ModificationCancelledEvent>(OnModificationCancelled);
    }

    private static void OnModificationCancelled(ModificationCancelledEvent ev)
    {
        ev.Component.CancelToken = null;
    }

    private void OnModificationComplete(ModificationCompleteEvent ev)
    {
        ev.Component.CancelToken = null;
        UseDNASequencer(ev.Target, ev.User, ev.Component);
    }

    private void UseDNASequencer(EntityUid target, EntityUid user, DNASequencerComponent component)
    {
        // Handle injecting/drawing for solutions
        if (component.ToggleState == SharedDNASequencerComponent.DNASequencerToggleMode.Modify)
        {
            if (TryComp<GeneticSequenceComponent>(target, out var geneticSequence))
            {
                TryModifyGenes(component, target, user);
            }
            else
            {
                _popup.PopupEntity(Loc.GetString("injector-component-cannot-transfer-message",
                    ("target", Identity.Entity(target, EntityManager))), component.Owner, user);
            }
        }
        else if (component.ToggleState == SharedDNASequencerComponent.DNASequencerToggleMode.Sequence)
        {
            // Draw from a bloodstream, if the target has that
            if (TryComp<GeneticSequenceComponent>(target, out var geneticSequence))
            {
                TrySequence(component, target, geneticSequence, user);
                return;
            }
            else
            {
                _popup.PopupEntity(Loc.GetString("injector-component-cannot-draw-message",
                    ("target", Identity.Entity(target, EntityManager))), component.Owner, user);
            }
        }
    }

    private static void OnDNASequencerDeselected(EntityUid uid, DNASequencerComponent component, HandDeselectedEvent args)
    {
        component.CancelToken?.Cancel();
        component.CancelToken = null;
    }

    private void OnDNASequenceChange(EntityUid uid, DNASequencerComponent component, DNASequenceChangedEvent args)
    {
        Dirty(component);
    }

    private void OnDNASequencerGetState(EntityUid uid, DNASequencerComponent component, ref ComponentGetState args)
    {
        args.State = new SharedDNASequencerComponent.DNASequencerComponentState(component.Sample, component.ToggleState);
    }

    private void OnDNASequencerAfterInteract(EntityUid uid, DNASequencerComponent component, AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        if (component.CancelToken != null)
        {
            args.Handled = true;
            return;
        }

        //Make sure we have the attacking entity
        if (args.Target is not { Valid: true } target ||
            !HasComp<DNASequencerComponent>(uid))
        {
            return;
        }

        // Is the target a mob? If yes, use a do-after to give them time to respond.
        if (HasComp<MobStateComponent>(target) ||
            HasComp<GeneticSequenceComponent>(target))
        {

            ModifyDoAfter(component, args.User, target);
            args.Handled = true;
            return;
        }

        UseDNASequencer(target, args.User, component);
        args.Handled = true;
    }

    private void OnDNASequencerStartup(EntityUid uid, DNASequencerComponent component, ComponentStartup args)
    {
        /// ???? why ?????
        Dirty(component);
    }

    private void OnDNASequencerUse(EntityUid uid, DNASequencerComponent component, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        RaiseLocalEvent(uid, new DNASequenceChangedEvent(component.Sample));

        Toggle(component, args.User);
        args.Handled = true;
    }

    /// <summary>
    /// Toggle between draw/inject state if applicable
    /// </summary>
    private void Toggle(DNASequencerComponent component, EntityUid user)
    {

        string msg;
        switch (component.ToggleState)
        {
            case SharedDNASequencerComponent.DNASequencerToggleMode.Modify:
                component.ToggleState = SharedDNASequencerComponent.DNASequencerToggleMode.Sequence;
                msg = "injector-component-drawing-text";
                break;
            case SharedDNASequencerComponent.DNASequencerToggleMode.Sequence:
                component.ToggleState = SharedDNASequencerComponent.DNASequencerToggleMode.Modify;
                msg = "injector-component-injecting-text";
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        _popup.PopupEntity(Loc.GetString(msg), component.Owner, user);
    }

    /// <summary>
    /// Send informative pop-up messages and wait for a do-after to complete.
    /// </summary>
    private void ModifyDoAfter(DNASequencerComponent component, EntityUid user, EntityUid target)
    {
        // Create a pop-up for the user
        _popup.PopupEntity(Loc.GetString("injector-component-injecting-user"), target, user);


        var actualDelay = MathF.Max(component.Delay, 1f);

        if (user != target)
        {
            // Create a pop-up for the target
            var userName = Identity.Entity(user, EntityManager);
            _popup.PopupEntity(Loc.GetString("injector-component-injecting-target",
                ("user", userName)), user, target);

            // Check if the target is incapacitated or in combat mode and modify time accordingly.
            if (_mobState.IsIncapacitated(target))
            {
                actualDelay /= 2;
            }
            else if (_combat.IsInCombatMode(target))
            {
                // Slightly increase the delay when the target is in combat mode. Helps prevents cheese injections in
                // combat with fast syringes & lag.
                actualDelay += 1;
            }

            // Add an admin log, using the "force feed" log type. It's not quite feeding, but the effect is the same.
            if (component.ToggleState == SharedDNASequencerComponent.DNASequencerToggleMode.Modify)
            {
                _adminLogger.Add(LogType.ForceFeed,
                    $"{EntityManager.ToPrettyString(user):user} is attempting to modify {EntityManager.ToPrettyString(target):target}'s DNA.");
            }
        }
        else
        {
            // Self-injections take half as long.
            actualDelay /= 2;

            if (component.ToggleState == SharedDNASequencerComponent.DNASequencerToggleMode.Modify)
                _adminLogger.Add(LogType.Ingestion,
                    $"{EntityManager.ToPrettyString(user):user} is attempting to modify their own DNA.");
        }

        component.CancelToken = new CancellationTokenSource();

        _doAfter.DoAfter(new DoAfterEventArgs(user, actualDelay, component.CancelToken.Token, target)
        {
            BreakOnUserMove = true,
            BreakOnDamage = true,
            BreakOnStun = true,
            BreakOnTargetMove = true,
            MovementThreshold = 0.1f,
            BroadcastFinishedEvent = new ModificationCompleteEvent()
            {
                Component = component,
                User = user,
                Target = target,
            },
            BroadcastCancelledEvent = new ModificationCancelledEvent()
            {
                Component = component,
            }
        });
    }

    private void TryModifyGenes(DNASequencerComponent component, EntityUid target, EntityUid user)
    {

        component.ToggleState = SharedDNASequencerComponent.DNASequencerToggleMode.Sequence;
        if (component.Sample == null)
        {
            return;
        }
        _genetics.ModifyGenes(target, component.Sample, GeneModificationMethod.DNASequencer);

        /*
        _popup.PopupEntity(Loc.GetString("injector-component-inject-success-message",
                ("amount", removedSolution.Volume),
                ("target", Identity.Entity(targetBloodstream.Owner, EntityManager))), component.Owner, user);
        */

        Dirty(component);
    }

    private void AfterSequence(DNASequencerComponent component)
    {
        component.ToggleState = SharedDNASequencerComponent.DNASequencerToggleMode.Modify;
    }

    private void TrySequence(DNASequencerComponent component, EntityUid targetEntity, GeneticSequenceComponent geneticSequence, EntityUid user)
    {

        component.Sample = geneticSequence.Genes;

        /*
        _popup.PopupEntity(Loc.GetString("injector-component-draw-success-message",
                ("amount", removedSolution.Volume),
                ("target", Identity.Entity(targetEntity, EntityManager))), component.Owner, user);
        */

        Dirty(component);
        AfterSequence(component);
    }

    private sealed class ModificationCompleteEvent : EntityEventArgs
    {
        public DNASequencerComponent Component { get; init; } = default!;
        public EntityUid User { get; init; }
        public EntityUid Target { get; init; }
    }

    private sealed class ModificationCancelledEvent : EntityEventArgs
    {
        public DNASequencerComponent Component { get; init; } = default!;
    }
}
