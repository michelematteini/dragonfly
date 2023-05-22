using System;
using System.Collections.Generic;
using Dragonfly.Utils;
using System.Linq;

namespace Dragonfly.Graphics.API.Common
{
    /// <summary>
    /// Coordinate command lists execution taking their prerequisites into account and implementing thread safety.
    /// </summary>
    internal class CmdListCoordinator
    {
        struct CmdListDecl
        {
            public GraphicResourceID ID;
            public IReadOnlyList<GraphicResourceID> RequiredIDs;
        }

        private object CMDLIST_SYNC; // used to synchronize command list on the main context
        private Dictionary<GraphicResourceID, CmdListDecl> lists; // all lists declared for this frame
        private HashSet<GraphicResourceID> allRequirements; // a set which merge all the required lists by all passes (used to solve the graph stages)
        private HashSet<GraphicResourceID> staged; // lists that have already been organized into stages
        private HashSet<GraphicResourceID> waitingLists; // list that have been queued for execution, but cannot be executed because required lists have still to be queued
        private ObjectPool<HashSet<GraphicResourceID>> stagePool; // pool to generate lists to be used to store toghether lists that should be executed in parallel
        private List<HashSet<GraphicResourceID>> stages; // sets of cmd lists that should be executed toghether on this frame

        public CmdListCoordinator()
        {
            CMDLIST_SYNC = new object();
            lists = new Dictionary<GraphicResourceID, CmdListDecl>();
            waitingLists = new HashSet<GraphicResourceID>();
            staged = new HashSet<GraphicResourceID>();
            stagePool = new ObjectPool<HashSet<GraphicResourceID>>(() => new HashSet<GraphicResourceID>(), l => l.Clear());
            stages = new List<HashSet<GraphicResourceID>>();
            allRequirements = new HashSet<GraphicResourceID>();
            ToBeExecuted = new BlockingQueue<HashSet<GraphicResourceID>>();
        }

        /// <summary>
        /// List of queues ready to be executed
        /// </summary>
        public BlockingQueue<HashSet<GraphicResourceID>> ToBeExecuted { get; private set; }

        /// <summary>
        /// Set to true when all the command lists have been executed for this frame.
        /// </summary>
        public bool EndOfFrame
        {
            get
            {
                lock(CMDLIST_SYNC)
                {
                    return stages.Count == 0 && ToBeExecuted.Count == 0;
                }
            }
        }

        public void NewFrame()
        {
            lock (CMDLIST_SYNC)
            {
                lists.Clear();
                waitingLists.Clear();
                staged.Clear();
                stages.Clear();
                stagePool.FreeAll();
                ToBeExecuted.Clear();
            }
        }

        /// <summary>
        /// Call this function to signal that the given command list will be executed this frame.
        /// </summary>
        public void DeclareList(GraphicResourceID cmdListID, IReadOnlyList<GraphicResourceID> requiredListIDs)
        {
#if DEBUG
            if (cmdListID == null)
                throw new ArgumentNullException();
#endif
            lock (CMDLIST_SYNC)
            {
                lists.Add(cmdListID, new CmdListDecl() { ID = cmdListID, RequiredIDs = requiredListIDs });
                for (int i = 0; i < requiredListIDs.Count; i++)
                    allRequirements.Add(requiredListIDs[i]);
            }
        }

        /// <summary>
        /// Solve the dependencies between the declared lists, separating them in stages that can be executed in parallel.
        /// This should be called after each group of lists that should precede the others has been declared, and at least once after all the lists have been declared.
        /// </summary>
        public void SolveRenderStages()
        {
            lock (CMDLIST_SYNC)
            {
                int stageID = 0;

                // find the last stage (a cmd list not required by any other)
                {
                    foreach (CmdListDecl cmdList in lists.Values)
                    {
                        if (staged.Contains(cmdList.ID))
                            continue; // already staged


                        if (allRequirements.Contains(cmdList.ID))
                            continue; // required by another, cannot be the last stage

#if DEBUG
                        if (stageID > 0)
                            throw new Exception("Invalid Command Lists: more than one command list is not required by the others: the dependecies between them must form a single tree!");
#endif

                        // push the last stage made by a single list
                        HashSet<GraphicResourceID> lastStage = stagePool.CreateNew();
                        lastStage.Add(cmdList.ID);
                        stages.Insert(stageID++, lastStage);
                        staged.Add(cmdList.ID);
#if !DEBUG
                        break;
#endif
                    }
                    allRequirements.Clear();
                }

                // iteratively fill the other stages
                while (true)
                {
                    // fill a new stage with  all the requirements from the current
                    HashSet<GraphicResourceID> prevStage = stages[stageID - 1], curStage = stagePool.CreateNew();
                    foreach (GraphicResourceID id in prevStage)
                    {
                        IReadOnlyList<GraphicResourceID> requiredIDs = lists[id].RequiredIDs;
                        for (int i = 0; i < requiredIDs.Count; i++)
                            curStage.Add(requiredIDs[i]);
                    }

                    // if there are no other requirements, the previous stage was the last one
                    if (curStage.Count == 0)
                        break;

                    // remove these new requirements from the already stacked stages
                    foreach (GraphicResourceID id in curStage)
                    {
                        for (int i = 0; i < stageID; i++)
                            stages[i].Remove(id);
                    }

                    // push the new stage
                    stages.Insert(stageID++, curStage);
                    foreach (GraphicResourceID id in curStage)
                        staged.Add(id);
                }

                QueueStagesForExecution();          
            }
        }

        public void QueueExecution(GraphicResourceID cmdListID)
        {
            lock (CMDLIST_SYNC)
            {
#if DEBUG
                if (cmdListID == null)
                    throw new ArgumentNullException();
                if (!lists.ContainsKey(cmdListID))
                    throw new InvalidOperationException("QueueExecution() cannot be called on a list that has not been declared with DeclareList()!");
                if (waitingLists.Contains(cmdListID))
                    throw new InvalidOperationException("QueueExecution() cannot be called twice on the same list!");
#endif
                waitingLists.Add(cmdListID);

                QueueStagesForExecution();
            }
        }

        private void QueueStagesForExecution()
        {
            // execute all stages for which all the lists have been queued
            while (stages.Count > 0 && stages.Last().IsSubsetOf(waitingLists))
            {
                HashSet<GraphicResourceID> readyStage = stages.Pop(); // pop the stage to be executed
                foreach (GraphicResourceID id in readyStage) // remove its lists from the waiting queue
                    waitingLists.Remove(id); 
                ToBeExecuted.Enqueue(readyStage); // queue the stage for execution
            }
        }

    }
}
