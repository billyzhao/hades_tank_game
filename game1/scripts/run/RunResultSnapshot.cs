using System;
using System.Collections.Generic;

namespace Game1;

/// <summary>结果界面使用的只读本局快照，UI 不得直接持有或修改 RunState。</summary>
public readonly record struct RunResultSnapshot(int Seed, IReadOnlyList<string> ProtocolIds, int RelayIntegrity, TimeSpan Elapsed);
