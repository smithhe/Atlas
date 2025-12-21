import type { Project, Risk, Settings, Task, TeamMember } from './types'

function isoNowMinusDays(days: number) {
  const d = new Date()
  d.setDate(d.getDate() - days)
  return d.toISOString()
}

export function seedTasks(): Task[] {
  return [
    {
      id: 'task-1',
      title: 'Review refactor proposal',
      priority: 'High',
      estimatedDurationText: '2d',
      estimateConfidence: 'Medium',
      actualDurationText: '',
      project: 'Core Platform',
      risk: 'Refactor justification',
      notes: 'Focus: validate scope, sequencing, and ROI. Capture assumptions + risks.',
      lastTouchedIso: isoNowMinusDays(1),
    },
    {
      id: 'task-2',
      title: 'Update onboarding docs',
      priority: 'Medium',
      estimatedDurationText: '1d',
      estimateConfidence: 'Low',
      actualDurationText: '',
      project: 'DevEx',
      risk: 'Onboarding drift',
      notes: 'Collect top “gotchas” from the last two onboards. Add checklists.',
      lastTouchedIso: isoNowMinusDays(11),
    },
    {
      id: 'task-3',
      title: 'Review pull requests',
      priority: 'Medium',
      estimatedDurationText: '2h',
      estimateConfidence: 'High',
      actualDurationText: '',
      notes: 'Batch reviews. Leave clear next steps for each PR.',
      lastTouchedIso: isoNowMinusDays(0),
    },
    {
      id: 'task-4',
      title: 'Triage flaky test failure',
      priority: 'Low',
      estimatedDurationText: '45m',
      estimateConfidence: 'Medium',
      actualDurationText: '',
      project: 'DevEx',
      notes: 'Quick repro + isolate recent changes. Capture next steps.',
      // With staleDays=10, this lands in the "yellow" band (within 2 days of stale).
      lastTouchedIso: isoNowMinusDays(9),
    },
    {
      id: 'task-5',
      title: 'Prep 1:1 agenda for next week',
      priority: 'Low',
      estimatedDurationText: '30m',
      estimateConfidence: 'High',
      actualDurationText: '',
      notes: 'Draft topics + collect feedback items.',
      lastTouchedIso: isoNowMinusDays(2),
    },
    {
      id: 'task-6',
      title: 'Review risk mitigations backlog',
      priority: 'Medium',
      estimatedDurationText: '1h',
      estimateConfidence: 'Medium',
      actualDurationText: '',
      project: 'Core Platform',
      risk: 'Refactor justification',
      notes: 'Identify next concrete mitigation steps and owners.',
      lastTouchedIso: isoNowMinusDays(6),
    },
  ]
}

export function seedTeam(): TeamMember[] {
  return [
    {
      id: 'tm-alice',
      name: 'Alice',
      role: 'Senior Engineer',
      statusDot: 'Green',
      currentFocus: 'Checkout flow performance',
      notes: [
        {
          id: 'note-a7',
          createdIso: isoNowMinusDays(0),
          tag: 'Standup',
          text:
            'Today: validate p95 improvement after the query batching change.\n' +
            'Next: re-run load test with realistic cart sizes and confirm we did not regress error rates.\n' +
            'Watchouts: cache invalidation + retry storms if downstream starts timing out.',
        },
        {
          id: 'note-a6',
          createdIso: isoNowMinusDays(1),
          tag: 'Concern',
          text:
            'We still see sporadic spikes in checkout latency when inventory calls are slow.\n' +
            'Hypothesis: fallback path is doing extra DB reads + serializing large payloads.\n' +
            'Action: capture a trace sample set and compare "fast" vs "slow" requests.',
          adoWorkItemId: '12345',
        },
        {
          id: 'note-a5',
          createdIso: isoNowMinusDays(3),
          tag: 'Praise',
          text:
            'Excellent collaboration with QA on reproducing the worst-case cart scenario.\n' +
            'The repro doc is clear and should help future on-calls.\n' +
            'Keep doing write-ups like this — they scale.',
        },
        {
          id: 'note-a4',
          createdIso: isoNowMinusDays(5),
          tag: 'Progress',
          text:
            'Identified top contributors to checkout time:\n' +
            '- redundant product lookups per line item\n' +
            '- N+1 tax calculation calls\n' +
            '- serialization overhead in the response builder\n' +
            'Next step: land batching behind a feature flag and monitor.',
          adoWorkItemId: '12345',
        },
        {
          id: 'note-a3',
          createdIso: isoNowMinusDays(7),
          tag: 'Standup',
          text:
            'Blocked briefly by flaky perf env; workaround was to pin the dataset snapshot.\n' +
            'Follow-up: we should automate dataset refresh + pinning so perf runs are reproducible.',
        },
        {
          id: 'note-a2',
          createdIso: isoNowMinusDays(9),
          tag: 'Blocker',
          text:
            'Load test run failed due to intermittent 502s from the gateway.\n' +
            'Need infra assist to confirm whether this is rate limiting or an upstream timeout.\n' +
            'In the meantime: run smaller-scale tests locally and capture traces.',
          prUrl: '(placeholder)',
        },
        {
          id: 'note-a1',
          createdIso: isoNowMinusDays(2),
          tag: 'Progress',
          text: 'Great progress on profiling + query batching.',
          adoWorkItemId: '12345',
        },
      ],
      azureItems: [
        {
          id: 'ado-12345',
          title: 'Reduce checkout latency',
          status: 'In Progress',
          timeTaken: '3d (est.)',
          ticketUrl: '(placeholder)',
          prUrl: '(placeholder)',
          commitsUrl: '(placeholder)',
        },
      ],
    },
    {
      id: 'tm-bob',
      name: 'Bob',
      role: 'Engineer',
      statusDot: 'Yellow',
      currentFocus: 'Refactor proposal review + instrumentation',
      notes: [
        {
          id: 'note-b1',
          createdIso: isoNowMinusDays(10),
          tag: 'Concern',
          text: 'Needs tighter definition of “done” before starting refactor.',
          prUrl: '(placeholder)',
        },
      ],
      azureItems: [
        {
          id: 'ado-22401',
          title: 'Add request tracing in API gateway',
          status: 'In Progress',
          ticketUrl: '(placeholder)',
          prUrl: '(placeholder)',
          commitsUrl: '(placeholder)',
        },
      ],
    },
    {
      id: 'tm-charlie',
      name: 'Charlie',
      role: 'Engineer',
      statusDot: 'Green',
      currentFocus: 'On-call stability',
      notes: [],
      azureItems: [],
    },
    {
      id: 'tm-dana',
      name: 'Dana',
      role: 'Tech Lead',
      statusDot: 'Green',
      currentFocus: 'Project planning + dependencies',
      notes: [
        {
          id: 'note-d1',
          createdIso: isoNowMinusDays(1),
          tag: 'Standup',
          text: 'Flagged cross-team dependency risk; will schedule alignment.',
        },
      ],
      azureItems: [
        {
          id: 'ado-99881',
          title: 'Q1 roadmap draft',
          status: 'In Review',
          ticketUrl: '(placeholder)',
          prUrl: '(placeholder)',
          commitsUrl: '(placeholder)',
        },
      ],
    },
    {
      id: 'tm-evan',
      name: 'Evan',
      role: 'Engineer',
      statusDot: 'Red',
      currentFocus: 'Blocked on environment flakiness',
      notes: [
        {
          id: 'note-e1',
          createdIso: isoNowMinusDays(3),
          tag: 'Blocker',
          text: 'CI environment flakes on integration tests; needs triage.',
        },
      ],
      azureItems: [
        {
          id: 'ado-55002',
          title: 'Stabilize integration tests',
          status: 'In Progress',
          timeTaken: '2d (so far)',
          ticketUrl: '(placeholder)',
          prUrl: '(placeholder)',
          commitsUrl: '(placeholder)',
        },
      ],
    },
  ]
}

export function seedRisks(): Risk[] {
  return [
    {
      id: 'risk-1',
      title: 'Refactor justification',
      status: 'Open',
      severity: 'High',
      project: 'Core Platform',
      description: 'We may spend multiple sprints refactoring without clear ROI or success metrics.',
      evidence: 'Prior refactors expanded scope; uncertain perf wins; unclear deprecation plan.',
      linkedTaskIds: ['task-1'],
      linkedTeamMemberIds: ['tm-bob', 'tm-dana'],
      history: [
        {
          id: 'rh-1',
          createdIso: isoNowMinusDays(5),
          text: 'Risk created after refactor proposal discussion.',
        },
      ],
      lastUpdatedIso: isoNowMinusDays(2),
    },
    {
      id: 'risk-2',
      title: 'Onboarding drift',
      status: 'Watching',
      severity: 'Medium',
      project: 'DevEx',
      description: 'Onboarding docs lag behind current reality, increasing time-to-productivity.',
      evidence: 'Multiple new hires hit the same setup issues; tribal knowledge in DMs.',
      linkedTaskIds: ['task-2'],
      linkedTeamMemberIds: ['tm-alice', 'tm-charlie'],
      history: [
        {
          id: 'rh-2',
          createdIso: isoNowMinusDays(12),
          text: 'Noticed repeated setup failures in week 1.',
        },
      ],
      lastUpdatedIso: isoNowMinusDays(11),
    },
  ]
}

export function seedProjects(): Project[] {
  return [
    {
      id: 'proj-1',
      name: 'Core Platform',
      summary: 'Shared services, reliability, and performance initiatives.',
      linkedTaskIds: ['task-1', 'task-3'],
      linkedRiskIds: ['risk-1'],
      teamMemberIds: ['tm-alice', 'tm-bob', 'tm-dana'],
    },
    {
      id: 'proj-2',
      name: 'DevEx',
      summary: 'Developer experience: tooling, onboarding, and CI hygiene.',
      linkedTaskIds: ['task-2'],
      linkedRiskIds: ['risk-2'],
      teamMemberIds: ['tm-charlie', 'tm-evan'],
    },
  ]
}

export function seedSettings(): Settings {
  return {
    staleDays: 10,
    defaultAiManualOnly: true,
    theme: 'Dark',
    azureDevOpsBaseUrl: '(placeholder)',
  }
}


