import { createHashRouter } from 'react-router-dom'
import { ShellLayout } from '../components/ShellLayout'
import { DashboardView } from '../views/DashboardView'
import { TasksView } from '../views/TasksView'
import { TeamView } from '../views/TeamView'
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
      { path: 'risks', element: <RisksView /> },
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


