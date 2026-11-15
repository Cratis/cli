// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.given;

public class an_old_copilot_installation : a_copilot_corpus
{
    void Establish()
    {
        AiCorpusSynchronizer.Synchronize(_project, _corpus, Configuration);
        AddAdapter();
    }
}
