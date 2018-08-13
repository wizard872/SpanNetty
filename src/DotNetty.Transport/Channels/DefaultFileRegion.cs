﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Transport.Channels
{
    using System;
    using System.Diagnostics.Contracts;
    using System.IO;
    using DotNetty.Common;
    using DotNetty.Common.Internal.Logging;
    using DotNetty.Common.Utilities;

    public class DefaultFileRegion : AbstractReferenceCounted, IFileRegion
    {
        static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance<DefaultFileRegion>();

        readonly FileStream file;

        public DefaultFileRegion(FileStream file, long position, long count)
        {
            Contract.Requires(file != null && file.CanRead);
            Contract.Requires(position >= 0 && count >= 0);
           
            this.file = file;
            this.Position = position;
            this.Count = count;
        }

        public override IReferenceCounted Touch(object hint) => this;

        public long Position { get; }

        public long Transferred { get; set; }

        public long Count { get; }

        public long TransferTo(Stream target, long pos)
        {
            Contract.Requires(target != null);
            Contract.Requires(pos >= 0);

            long totalCount = this.Count - pos;
            if (totalCount < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException_Position(pos, this.Count);
            }

            if (totalCount == 0)
            {
                return 0L;
            }
            if (this.ReferenceCount == 0)
            {
                ThrowHelper.ThrowIllegalReferenceCountException(0);
            }

            var buffer = new byte[totalCount];
            int total = this.file.Read(buffer, (int)(this.Position + pos), (int)totalCount);
            target.Write(buffer, 0, total);
            if (total > 0)
            {
                this.Transferred += total;
            }

            return total;
        }

        protected override void Deallocate()
        {
            FileStream fileStream = this.file;
            if (!fileStream.CanRead)
            {
                return;
            }

            try
            {
                fileStream.Dispose();
            }
            catch (Exception exception)
            {
                if (Logger.WarnEnabled)
                {
                    Logger.FailedToCloseAFileStream(exception);
                }
            }
        }
    }
}
