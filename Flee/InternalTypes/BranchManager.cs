using System.Reflection.Emit;

namespace Flee.InternalTypes
{
    /// <summary>
    /// Manages branch information and decides whether to emit a short or long branch for each
    /// recorded jump. Drives the two-pass emit performed by <see cref="Expression{T}.Compile"/>.
    /// </summary>
    internal class BranchManager
    {
        private readonly IList<BranchInfo> MyBranchInfos;

        /// <summary>
        /// Initializes an empty manager.
        /// </summary>
        public BranchManager()
        {
            MyBranchInfos = [];
        }

        /// <summary>
        /// Returns whether any tracked branch currently exceeds the short-branch range.
        /// </summary>
        /// <returns><see langword="true"/> when at least one long branch is required.</returns>
        public bool HasLongBranches()
        {
            foreach (BranchInfo bi in MyBranchInfos)
            {
                if (bi.ComputeIsLongBranch())
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Determines whether each branch should use the short or long opcode form, and
        /// shifts subsequent IL positions to account for long branches.
        /// </summary>
        /// <returns><see langword="true"/> when at least one long branch was emitted.</returns>
        public bool ComputeBranches()
        {
            //
            // we need to iterate in reverse order of the
            // starting location, as branch between our
            // branch could push our branch to a long branch.
            //
            for (var idx = MyBranchInfos.Count - 1; idx >= 0; idx--)
            {
                var bi = MyBranchInfos[idx];

                // count long branches between
                int longBranchesBetween = 0;
                for (var ii = idx + 1; ii < MyBranchInfos.Count; ii++)
                {
                    var bi2 = MyBranchInfos[ii];
                    if (bi2.IsBetween(bi) && bi2.ComputeIsLongBranch())
                    {
                        ++longBranchesBetween;
                    }
                }

                // Adjust the branch as necessary
                bi.AdjustForLongBranchesBetween(longBranchesBetween);
            }

            int longBranchCount = 0;

            // Adjust the start location of each branch
            foreach (BranchInfo bi in MyBranchInfos)
            {
                // Save the short/long branch type
                bi.BakeIsLongBranch();

                // Adjust the start location as necessary
                bi.AdjustForLongBranches(longBranchCount);

                // Keep a tally of the number of long branches
                longBranchCount += Convert.ToInt32(bi.IsLongBranch);
            }

            return longBranchCount > 0;
        }

        /// <summary>
        /// Returns whether the branch starting at <paramref name="ilg"/>'s current position is
        /// long. Defaults to <see langword="true"/> for unknown branches so we err on the safe side.
        /// </summary>
        /// <param name="ilg">The IL generator whose current position is the branch start.</param>
        /// <returns><see langword="true"/> when long form should be emitted.</returns>
        public bool IsLongBranch(FleeILGenerator ilg)
        {
            ILLocation startLoc = new(ilg.Length);

            foreach (var bi in MyBranchInfos)
            {
                if (bi.Equals(startLoc))
                {
                    return bi.IsLongBranch;
                }
            }

            // we don't really know since this branch didn't exist.
            // we could throw an exceptio but
            // do a long branch to be safe.
            return true;
        }

        /// <summary>
        /// Records a branch from <paramref name="ilg"/>'s current position to <paramref name="target"/>.
        /// </summary>
        /// <param name="ilg">The IL generator providing the start position.</param>
        /// <param name="target">The label being branched to.</param>
        public void AddBranch(FleeILGenerator ilg, Label target)
        {
            ILLocation startLoc = new(ilg.Length);

            BranchInfo bi = new(startLoc, target);
            // branches will be sorted in order
            MyBranchInfos.Add(bi);
        }

        /// <summary>
        /// Resolves <paramref name="target"/> to <paramref name="ilg"/>'s current position
        /// across all tracked branches.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="target">The label being marked.</param>
        public void MarkLabel(FleeILGenerator ilg, Label target)
        {
            int pos = ilg.Length;

            foreach (BranchInfo bi in MyBranchInfos)
            {
                bi.Mark(target, pos);
            }
        }

        /// <summary>
        /// Returns a newline-separated diagnostic listing of every tracked branch.
        /// </summary>
        /// <returns>The diagnostic string.</returns>
        public override string ToString()
        {
            string[] arr = new string[MyBranchInfos.Count];

            for (int i = 0; i <= MyBranchInfos.Count - 1; i++)
            {
                arr[i] = MyBranchInfos[i].ToString();
            }

            return string.Join(Environment.NewLine, arr);
        }
    }
}
