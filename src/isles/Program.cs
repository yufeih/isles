// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

Directory.SetCurrentDirectory(AppContext.BaseDirectory);

using var game = new Isles.GameIsles();
game.Run();
