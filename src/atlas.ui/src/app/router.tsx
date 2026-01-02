import { createHashRouter } from 'react-router-dom'
import { ShellLayout } from '../components/ShellLayout'
import { DashboardView } from '../views/DashboardView'
import { TasksView } from '../views/TasksView'
import { TeamView } from '../views/TeamView'
import { TeamNoteDetailView } from '../views/TeamNoteDetailView'
import { TeamWorkItemDetailView } from '../views/TeamWorkItemDetailView'
import { TeamMemberRiskDetailView } from '../views/TeamMemberRiskDetailView'
import { GrowthGoalDetailView } from '../views/GrowthGoalDetailView'
import { RisksView } from '../views/RisksView'
import { ProjectsView } from '../views/ProjectsView'
import { SettingsView } from '../views/SettingsView'
import { NotFoundView } from '../views/NotFoundView'

const routerRoutes = [
  {
    path: '/',
    element: <ShellLayout />,
    children: [
      { index: true, element: <DashboardView /> },
      { path: 'tasks', element: <TasksView /> },
      { path: 'tasks/:taskId', element: <TasksView /> },
      { path: 'team', element: <TeamView /> },
      { path: 'team/:memberId', element: <TeamView /> },
      { path: 'team/:memberId/notes', element: <TeamView /> },
      { path: 'team/:memberId/notes/:noteId', element: <TeamNoteDetailView /> },
      { path: 'team/:memberId/work-items', element: <TeamView /> },
      { path: 'team/:memberId/work-items/:workItemId', element: <TeamWorkItemDetailView /> },
      { path: 'team/:memberId/risks', element: <TeamView /> },
      { path: 'team/:memberId/risks/:teamMemberRiskId', element: <TeamMemberRiskDetailView /> },
      { path: 'team/:memberId/growth', element: <TeamView /> },
      { path: 'team/:memberId/growth/goals/:goalId', element: <GrowthGoalDetailView /> },
      { path: 'risks', element: <RisksView /> },
      { path: 'risks/:riskId', element: <RisksView /> },
      { path: 'projects', element: <ProjectsView /> },
      { path: 'settings', element: <SettingsView /> },
      { path: '*', element: <NotFoundView /> },
    ],
  },
]

const routerOptions = {
  // react-router supports this flag, but the TS type in this version doesn't include it.
  future: ({
    v7_startTransition: true,
  } as unknown) as never,
}

export const appRouter = createHashRouter(routerRoutes, routerOptions)


