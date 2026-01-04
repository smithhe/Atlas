import type { Growth, Project, Risk, Settings, Task, TeamMember, TeamMemberRisk } from './types'

function isoNowMinusDays(days: number) {
  const d = new Date()
  d.setDate(d.getDate() - days)
  return d.toISOString()
}

function isoDateNowMinusDays(days: number) {
  return isoNowMinusDays(days).slice(0, 10)
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
      profile: {
        timeZone: 'PT',
        typicalHours: '9–5',
      },
      signals: {
        load: 'Heavy',
        delivery: 'OnTrack',
        supportNeeded: 'Low',
      },
      pinnedNoteIds: ['note-a7', 'note-a6', 'note-a5', 'note-a4', 'note-a2'],
      activitySnapshot: {
        bullets: [
          'Key wins / impact: checkout p95 improvements trending in the right direction after batching work.',
          'Repeated friction: intermittent gateway 502s make perf testing unreliable.',
          'Collaboration patterns: strong QA partnership on repro + docs.',
          'Trends to track: inventory latency spikes and retry storms.',
        ],
        lastUpdatedIso: isoNowMinusDays(1),
        quickTags: ['Perf', 'Reliability'],
      },
      notes: [
        {
          id: 'note-a0-markdown',
          createdIso: isoNowMinusDays(0),
          tag: 'Quick',
          text:
            '# Markdown demo\n' +
            '\n' +
            'This note supports **bold**, _italic_, and `inline code`.\n' +
            '\n' +
            '## Lists\n' +
            '- Bullet one\n' +
            '- Bullet two\n' +
            '  - Nested bullet\n' +
            '\n' +
            '## Task list (GFM)\n' +
            '- [x] Shipped initial Markdown rendering\n' +
            '- [ ] Add editor preview toggle (later)\n' +
            '\n' +
            '## Code block\n' +
            '```ts\n' +
            'function greet(name: string) {\n' +
            "  return `Hello, ${name}!`\n" +
            '}\n' +
            '```\n' +
            '\n' +
            'Link: [React Markdown](https://github.com/remarkjs/react-markdown)\n',
        },
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
          prUrls: ['(placeholder)', '(placeholder-2)'],
          commitsUrl: '(placeholder)',
          assignedTo: 'Alice',
          startDateIso: new Date(Date.now() - 1000 * 60 * 60 * 24 * 6).toISOString().slice(0, 10),
          projectId: 'proj-1',
          localNotes: [
            {
              id: 'win-seed-1',
              createdIso: isoNowMinusDays(1),
              text: 'Local note: watch p95 checkout, confirm inventory fallback path is not regressing.',
            },
          ],
          history: [
            { id: 'wih-seed-1', createdIso: isoNowMinusDays(6), kind: 'StateChanged', summary: 'State changed to In Progress' },
            { id: 'wih-seed-2', createdIso: isoNowMinusDays(6), kind: 'AssignedToChanged', summary: 'Assigned to Alice' },
            { id: 'wih-seed-3', createdIso: isoNowMinusDays(2), kind: 'PullRequestLinked', summary: '2 pull requests linked' },
          ],
        },
        {
          id: 'ado-13002',
          title: 'Inventory service timeouts during checkout (investigate + mitigate)',
          status: 'Blocked',
          timeTaken: '1d (so far)',
          ticketUrl: '(placeholder)',
          prUrls: ['(placeholder)'],
          commitsUrl: '(placeholder)',
          assignedTo: 'Alice',
          startDateIso: new Date(Date.now() - 1000 * 60 * 60 * 24 * 2).toISOString().slice(0, 10),
          projectId: 'proj-1',
        },
      ],
    },
    {
      id: 'tm-bob',
      name: 'Bob',
      role: 'Engineer',
      statusDot: 'Yellow',
      currentFocus: 'Refactor proposal review + instrumentation',
      profile: {
        timeZone: 'ET',
        typicalHours: '10–6',
      },
      signals: {
        load: 'Normal',
        delivery: 'OnTrack',
        supportNeeded: 'Medium',
      },
      pinnedNoteIds: ['note-b1'],
      activitySnapshot: {
        bullets: ['Focus: clarify success metrics for the refactor proposal before execution.'],
        lastUpdatedIso: isoNowMinusDays(3),
        quickTags: ['Planning'],
      },
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
          prUrls: ['(placeholder)'],
          commitsUrl: '(placeholder)',
          assignedTo: 'Bob',
          projectId: 'proj-2',
        },
      ],
    },
    {
      id: 'tm-charlie',
      name: 'Charlie',
      role: 'Engineer',
      statusDot: 'Green',
      currentFocus: 'On-call stability',
      profile: {
        timeZone: 'CT',
        typicalHours: '9–5',
      },
      signals: {
        load: 'Light',
        delivery: 'OnTrack',
        supportNeeded: 'Low',
      },
      pinnedNoteIds: [],
      activitySnapshot: {
        bullets: ['Keeping services stable during on-call; no major incidents in the last month.'],
        lastUpdatedIso: isoNowMinusDays(5),
        quickTags: ['OnCall'],
      },
      notes: [],
      azureItems: [],
    },
    {
      id: 'tm-dana',
      name: 'Dana',
      role: 'Tech Lead',
      statusDot: 'Green',
      currentFocus: 'Project planning + dependencies',
      profile: {
        timeZone: 'PT',
        typicalHours: '8–4',
      },
      signals: {
        load: 'Heavy',
        delivery: 'AtRisk',
        supportNeeded: 'Medium',
      },
      pinnedNoteIds: ['note-d1'],
      activitySnapshot: {
        bullets: ['Raised cross-team dependency risk; scheduled alignment to unblock.'],
        lastUpdatedIso: isoNowMinusDays(1),
        quickTags: ['Roadmap', 'Deps'],
      },
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
          prUrls: ['(placeholder)'],
          commitsUrl: '(placeholder)',
          assignedTo: 'Dana',
          projectId: 'proj-1',
        },
      ],
    },
    {
      id: 'tm-evan',
      name: 'Evan',
      role: 'Engineer',
      statusDot: 'Red',
      currentFocus: 'Blocked on environment flakiness',
      profile: {
        timeZone: 'ET',
        typicalHours: '9–5',
      },
      signals: {
        load: 'Heavy',
        delivery: 'Blocked',
        supportNeeded: 'High',
      },
      pinnedNoteIds: ['note-e1'],
      activitySnapshot: {
        bullets: ['Repeated friction: CI flakiness on integration tests is blocking progress.'],
        lastUpdatedIso: isoNowMinusDays(2),
        quickTags: ['CI', 'FlakyTests'],
      },
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
          prUrls: ['(placeholder)'],
          commitsUrl: '(placeholder)',
        },
      ],
    },
  ]
}

export function seedGrowth(): Growth[] {
  return [
    {
      id: 'growth-alice',
      memberId: 'tm-alice',
      goals: [
        {
          id: 'growth-goal-alice-1',
          title: 'Move toward Technical Lead responsibilities',
          description: 'Increasing architectural ownership and supporting teammates during implementation.',
          status: 'OnTrack',
          category: 'Leadership',
          priority: 'Medium',
          startDateIso: isoDateNowMinusDays(30),
          targetDateIso: isoDateNowMinusDays(-60),
          lastUpdatedIso: isoNowMinusDays(2),
          progressPercent: 55,
          summary: 'Focus on taking clearer ownership of architecture decisions and helping unblock others during execution.',
          successCriteria: [
            'Lead at least one design discussion end-to-end each sprint',
            'Write/ship a short design note before major changes',
            'Proactively unblock teammates during implementation (pairing, reviews, clear next steps)',
          ],
          actions: [
            {
              id: 'growth-action-alice-1',
              title: 'Schedule a weekly 30m design sync for upcoming work',
              dueDateIso: isoDateNowMinusDays(7),
              state: 'InProgress',
              priority: 'Medium',
              notes: 'Keep it lightweight: agenda + decisions + next steps.',
              links: ['(doc placeholder) Design sync agenda template'],
            },
            {
              id: 'growth-action-alice-2',
              title: 'Draft a design note for the next checkout perf iteration',
              dueDateIso: isoDateNowMinusDays(14),
              state: 'Planned',
              priority: 'Medium',
              notes: 'Aim for 1–2 pages: context, options, tradeoffs, risks.',
              links: ['(doc placeholder) Design note template'],
            },
            {
              id: 'growth-action-alice-4',
              title: 'Run a 45m “alignment” session before starting the next cross-cutting change',
              dueDateIso: isoDateNowMinusDays(3),
              state: 'Complete',
              priority: 'Medium',
              notes: 'Outcome: agreed on scope boundaries + what “done” means.',
              links: ['(notes placeholder) Alignment summary'],
            },
            {
              id: 'growth-action-alice-5',
              title: 'Pair-review two PRs this sprint to model “why + tradeoffs” writeups',
              dueDateIso: isoDateNowMinusDays(1),
              state: 'InProgress',
              priority: 'Low',
              notes: 'Pick one medium change and one higher-risk change; focus on communicating edge cases.',
              links: ['(placeholder) PR-4821', '(placeholder) PR-4840'],
            },
            {
              id: 'growth-action-alice-6',
              title: 'Publish a short “decision log” note after each design decision',
              dueDateIso: isoDateNowMinusDays(-10),
              state: 'Planned',
              priority: 'Low',
              notes: 'Keep it to: decision + context + alternatives + why.',
              links: [],
            },
          ],
          checkIns: [
            {
              id: 'growth-checkin-alice-1',
              dateIso: isoDateNowMinusDays(10),
              signal: 'Positive',
              note: 'Had a strong architecture discussion and aligned early on tradeoffs; felt smoother than last sprint.',
            },
            {
              id: 'growth-checkin-alice-2',
              dateIso: isoDateNowMinusDays(3),
              signal: 'Mixed',
              note: 'Good progress, but I still waited too long to surface a dependency risk to the stakeholders.',
            },
            {
              id: 'growth-checkin-alice-4',
              dateIso: isoDateNowMinusDays(1),
              signal: 'Positive',
              note: 'Proactively pulled in two teammates early; pairing helped unblock and improved confidence in the approach.',
            },
            {
              id: 'growth-checkin-alice-5',
              dateIso: isoDateNowMinusDays(0),
              signal: 'Mixed',
              note: 'I led the decision, but the write-up was too sparse; reviewers asked about edge cases I hadn’t documented.',
            },
          ],
        },
        {
          id: 'growth-goal-alice-2',
          title: 'Improve cross-team communication',
          description: 'Sharing risks and decisions earlier with stakeholders and peers.',
          status: 'NeedsAttention',
          category: 'Communication',
          priority: 'High',
          startDateIso: isoDateNowMinusDays(20),
          targetDateIso: isoDateNowMinusDays(-40),
          lastUpdatedIso: isoNowMinusDays(6),
          progressPercent: 30,
          summary: 'Make risk/decision communication earlier and more consistent to reduce surprises and rework.',
          successCriteria: [
            'Flag risks within 24 hours of noticing them (in the right channel)',
            'Share key decisions with a short context + tradeoffs note',
          ],
          actions: [
            {
              id: 'growth-action-alice-3',
              title: 'Post a weekly “risks & decisions” update in the cross-team channel',
              dueDateIso: isoDateNowMinusDays(5),
              state: 'InProgress',
              priority: 'High',
              notes: 'Keep it to 3 bullets: risk, decision, ask.',
              links: ['(placeholder) #cross-team weekly update thread'],
            },
            {
              id: 'growth-action-alice-7',
              title: 'Add a “Risks / Watchouts” section to PR descriptions for higher-risk changes',
              dueDateIso: isoDateNowMinusDays(2),
              state: 'InProgress',
              priority: 'High',
              notes: 'Goal: make downstream impacts visible before review starts.',
              links: ['(placeholder) PR template snippet'],
            },
            {
              id: 'growth-action-alice-8',
              title: 'Share a one-paragraph “why now” context before asking for partner work',
              dueDateIso: isoDateNowMinusDays(8),
              state: 'Complete',
              priority: 'Medium',
              notes: 'Tested twice; got quicker responses when context was clear.',
              links: [],
            },
            {
              id: 'growth-action-alice-9',
              title: 'Ask one stakeholder: “Was anything surprising this week?”',
              dueDateIso: isoDateNowMinusDays(-7),
              state: 'Planned',
              priority: 'Low',
              notes: 'Capture patterns and adjust weekly update format.',
              links: [],
            },
          ],
          checkIns: [
            {
              id: 'growth-checkin-alice-3',
              dateIso: isoDateNowMinusDays(7),
              signal: 'Concern',
              note: 'I raised a major risk late; it caused replanning and made partners feel surprised.',
            },
            {
              id: 'growth-checkin-alice-6',
              dateIso: isoDateNowMinusDays(4),
              signal: 'Mixed',
              note: 'I posted the risk earlier this time, but the message was too long; I need a tighter format.',
            },
            {
              id: 'growth-checkin-alice-7',
              dateIso: isoDateNowMinusDays(2),
              signal: 'Positive',
              note: 'A short “decision + tradeoffs” note reduced back-and-forth and avoided rework.',
            },
          ],
        },
      ],
      skillsInProgress: [
        'System Design: Intermediate \u2192 Advanced',
        'Technical Communication: Developing \u2192 Solid',
        'Delegation: Early \u2192 Developing',
      ],
      feedbackThemes: [
        {
          id: 'growth-theme-alice-1',
          title: 'Strong execution, emerging leadership',
          description: 'Consistently delivers quality work and is beginning to unblock others.',
          observedSinceLabel: 'Observed since Jan',
        },
        {
          id: 'growth-theme-alice-2',
          title: 'Late risk communication',
          description: 'Risks are usually handled well, but surfacing them earlier would improve planning.',
          observedSinceLabel: 'Observed since Feb',
        },
      ],
      focusAreasMarkdown:
        '- Practicing earlier communication of technical risks during sprint execution\n' +
        '- Stepping into design discussions when ambiguity is high\n' +
        '- Delegating tasks during high workload instead of absorbing work\n',
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
      description:
        '## Summary\n' +
        '\n' +
        'We may spend multiple sprints refactoring without clear ROI or success metrics.\n' +
        '\n' +
        '### What we need before starting\n' +
        '- Define **success metrics** (e.g. p95 latency, error rate, cost).\n' +
        '- Agree on **scope boundaries** (what is explicitly *out of scope*).\n' +
        '- Write a short plan for **migration/deprecation**.\n' +
        '\n' +
        '> Suggestion: treat this like a product bet — make the hypothesis testable.\n',
      evidence:
        '### Signals we’ve seen\n' +
        '- Prior refactors expanded scope.\n' +
        '- Uncertain performance wins.\n' +
        '- Unclear deprecation plan.\n' +
        '\n' +
        '### Useful references\n' +
        '- [Refactor checklist](https://example.com) (placeholder)\n' +
        '\n' +
        '### Example acceptance criteria\n' +
        '```text\n' +
        'p95 checkout latency improves by 15% with no increase in error rate.\n' +
        '```',
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
    {
      id: 'risk-3',
      title: 'Sustained over-capacity across multiple streams',
      status: 'Watching',
      severity: 'High',
      project: 'Core Platform',
      description: 'Capacity constraints may create delivery risk and increase burnout probability if left unaddressed.',
      evidence: 'PR cycle time increasing; missed follow-ups due to context switching; rising WIP.',
      linkedTaskIds: [],
      linkedTeamMemberIds: [],
      history: [
        {
          id: 'rh-3',
          createdIso: isoNowMinusDays(4),
          text: 'Added as a global risk to track mitigation experiments (ownership + load balancing).',
        },
      ],
      lastUpdatedIso: isoNowMinusDays(3),
    },
  ]
}

export function seedTeamMemberRisks(): TeamMemberRisk[] {
  return [
    {
      id: 'tmrisk-1',
      memberId: 'tm-bob',
      title: 'Sustained Over-Capacity Across Multiple Streams',
      severity: 'High',
      riskType: 'Workload / Capacity',
      status: 'Mitigating',
      trend: 'Worsening',
      firstNoticedDateIso: isoDateNowMinusDays(21),
      impactArea: 'Delivery & Sustainability',
      description:
        'Engineer is currently owning three parallel initiatives across two teams. PR turnaround time has increased and context switching is causing missed follow-ups. Risk of burnout if load remains unchanged.',
      currentAction:
        'Reduced active work-in-progress to one primary feature. Secondary work deferred or reassigned. Weekly check-in added to reassess capacity.',
      lastReviewedIso: isoNowMinusDays(3),
      linkedRiskId: 'risk-3',
    },
    {
      id: 'tmrisk-2',
      memberId: 'tm-alice',
      title: 'Perf testing blocked by intermittent gateway errors',
      severity: 'Medium',
      riskType: 'Environment / Reliability',
      status: 'Open',
      trend: 'Stable',
      firstNoticedDateIso: isoDateNowMinusDays(14),
      impactArea: 'Delivery',
      description:
        'Intermittent gateway 502s are making performance test runs unreliable, slowing iteration and increasing the chance we ship without confidence in impact.',
      currentAction: 'Collect trace samples; coordinate with infra for rate limiting/timeouts; run smaller-scale tests locally as a stopgap.',
      lastReviewedIso: isoNowMinusDays(2),
      linkedRiskId: 'risk-1',
    },
    {
      id: 'tmrisk-3',
      memberId: 'tm-evan',
      title: 'Sustained blocking due to CI flakiness',
      severity: 'High',
      riskType: 'Tooling / CI',
      status: 'Mitigating',
      trend: 'Stable',
      firstNoticedDateIso: isoDateNowMinusDays(35),
      impactArea: 'Delivery & Sustainability',
      description: 'CI instability on integration tests is blocking progress and increasing frustration/interrupt load.',
      currentAction: 'Triage top flaky suites; isolate recent changes; schedule a dedicated stabilization sprint slice; add ownership rotation.',
      lastReviewedIso: isoNowMinusDays(1),
      linkedRiskId: 'risk-2',
    },
  ]
}

export function seedProjects(): Project[] {
  return [
    {
      id: 'proj-1',
      name: 'Core Platform',
      summary: 'Shared services, reliability, and performance initiatives.',
      description: 'Shared services, reliability, and performance initiatives across core systems.',
      status: 'Active',
      health: 'Green',
      targetDateIso: '2026-03-15',
      priority: 'High',
      productOwnerId: 'tm-dana',
      tags: ['platform', 'reliability', 'performance'],
      links: [
        { label: 'Spec', url: '(placeholder)' },
        { label: 'Dashboard', url: '(placeholder)' },
        { label: 'Repo', url: '(placeholder)' },
      ],
      lastUpdatedIso: isoNowMinusDays(0),
      latestCheckIn: {
        dateIso: '2025-12-30',
        note:
          'Checkout performance work is tracking; next focus is stabilizing inventory timeouts and tightening refactor success metrics.',
      },
      linkedTaskIds: ['task-1', 'task-3'],
      linkedRiskIds: ['risk-1'],
      teamMemberIds: ['tm-alice', 'tm-bob', 'tm-dana'],
    },
    {
      id: 'proj-2',
      name: 'DevEx',
      summary: 'Developer experience: tooling, onboarding, and CI hygiene.',
      description: 'Developer experience: onboarding, tooling, and CI stability improvements.',
      status: 'Active',
      health: 'Yellow',
      targetDateIso: '2026-02-07',
      priority: 'Medium',
      productOwnerId: 'tm-charlie',
      tags: ['tech-debt', 'ci', 'onboarding'],
      links: [
        { label: 'Spec', url: '(placeholder)' },
        { label: 'Dashboard', url: '(placeholder)' },
        { label: 'Repo', url: '(placeholder)' },
      ],
      lastUpdatedIso: isoNowMinusDays(2),
      latestCheckIn: {
        dateIso: '2025-12-29',
        note: 'Onboarding doc refresh in progress; CI flakiness mitigation plan drafted with ownership rotation.',
      },
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


