﻿using System.Threading.Tasks;

namespace DotNext.Net.Cluster.Consensus.Raft
{
    using Replication;

    /// <summary>
    /// Represents persistent state of local cluster member
    /// required by Raft consensus protocol.
    /// </summary>
    public interface IPersistentState : IAuditTrail<LogEntryId>
    {
        /// <summary>
        /// Determines whether the local member granted its vote for the specified remote member.
        /// </summary>
        /// <param name="member">The cluster member to check.</param>
        /// <returns><see langword="true"/> if the local member granted its vote for the specified remote member; otherwise, <see langword="false"/>.</returns>
        ValueTask<bool> IsVotedForAsync(IRaftClusterMember member);

        /// <summary>
        /// Reads Term value associated with the local member
        /// from the persistent storage.
        /// </summary>
        /// <returns>The term restored from persistent storage.</returns>
        ValueTask<long> RestoreTermAsync();

        /// <summary>
        /// Persists the last actual Term.
        /// </summary>
        /// <param name="term">The term value to be persisted.</param>
        /// <returns>The task representing asynchronous execution of the operation.</returns>
        ValueTask SaveTermAsync(long term);

        /// <summary>
        /// Persists the item that was voted for on in the last vote.
        /// </summary>
        /// <param name="member">The member which identifier should be stored inside of persistence storage. May be <see langword="null"/>.</param>
        /// <returns>The task representing asynchronous execution of the operation.</returns>
        ValueTask SaveVotedForAsync(IRaftClusterMember member);
    }
}
