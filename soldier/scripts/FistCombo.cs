using System;
namespace UnnamedFightingGame;

/// <summary>Three fresh-press attacks, one follow-up buffer, and a hard ending.</summary>
public sealed class FistCombo
{
    public const double GraceSeconds = .200;
    public const double CooldownSeconds = .150;
    private enum Phase { Ready, First, FirstGrace, Second, SecondGrace, Third, Cooldown }
    private Phase _phase;
    private double _left;
    public bool IsAttacking => AttackNumber != 0;
    public int AttackNumber => _phase switch { Phase.First=>1, Phase.Second=>2, Phase.Third=>3, _=>0 };
    public bool Queued { get; private set; }
    public bool Advance(double delta, bool pressed)
    {
        bool started = false;
        while (_phase != Phase.Ready && delta + 1e-7 >= _left)
        {
            delta = Math.Max(0,delta-_left);
            switch (_phase)
            {
                case Phase.First:
                    if (Queued) { Start(Phase.Second,SoldierVisual.CrossSeconds); started=true; }
                    else { Start(Phase.FirstGrace,GraceSeconds); started=false; }
                    Queued=false; break;
                case Phase.Second:
                    if (Queued) { Start(Phase.Third,SoldierVisual.KickSeconds); started=true; }
                    else { Start(Phase.SecondGrace,GraceSeconds); started=false; }
                    Queued=false; break;
                case Phase.Third: Start(Phase.Cooldown,CooldownSeconds); started=false; break;
                case Phase.FirstGrace:
                case Phase.SecondGrace:
                case Phase.Cooldown: _phase=Phase.Ready; started=false; break;
            }
        }
        if (_phase!=Phase.Ready) _left-=delta;
        if (pressed)
        {
            if (_phase is Phase.First or Phase.Second) Queued=true;
            else if (_phase==Phase.FirstGrace) { Start(Phase.Second,SoldierVisual.CrossSeconds); started=true; }
            else if (_phase==Phase.SecondGrace) { Start(Phase.Third,SoldierVisual.KickSeconds); started=true; }
            else if (_phase==Phase.Ready) { Start(Phase.First,SoldierVisual.AttackSeconds); started=true; }
        }
        return started;
    }
    private void Start(Phase phase,double seconds) { _phase=phase; _left=seconds; }
    public void Reset() { _phase=Phase.Ready; _left=0; Queued=false; }
}
