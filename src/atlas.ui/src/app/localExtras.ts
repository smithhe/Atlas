import type { TeamMember } from './types'
import { SeedIds } from './seedIds'

function isoNowMinusDays(days: number) {
  const d = new Date()
  d.setDate(d.getDate() - days)
  return d.toISOString()
}

export type TeamMemberLocalExtras = Pick<TeamMember, 'activitySnapshot' | 'azureItems'>

export const teamMemberLocalExtrasById: Record<string, TeamMemberLocalExtras> = {
  [SeedIds.teamMemberAlice]: {
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
        projectId: SeedIds.projectCorePlatform,
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
        projectId: SeedIds.projectCorePlatform,
      },
    ],
  },

  [SeedIds.teamMemberBob]: {
    activitySnapshot: {
      bullets: ['Focus: clarify success metrics for the refactor proposal before execution.'],
      lastUpdatedIso: isoNowMinusDays(3),
      quickTags: ['Planning'],
    },
    azureItems: [
      {
        id: 'ado-22401',
        title: 'Add request tracing in API gateway',
        status: 'In Progress',
        ticketUrl: '(placeholder)',
        prUrls: ['(placeholder)'],
        commitsUrl: '(placeholder)',
        assignedTo: 'Bob',
        projectId: SeedIds.projectDevEx,
      },
    ],
  },

  [SeedIds.teamMemberCharlie]: {
    activitySnapshot: {
      bullets: ['Keeping services stable during on-call; no major incidents in the last month.'],
      lastUpdatedIso: isoNowMinusDays(5),
      quickTags: ['OnCall'],
    },
    azureItems: [],
  },

  [SeedIds.teamMemberDana]: {
    activitySnapshot: {
      bullets: ['Raised cross-team dependency risk; scheduled alignment to unblock.'],
      lastUpdatedIso: isoNowMinusDays(1),
      quickTags: ['Roadmap', 'Deps'],
    },
    azureItems: [
      {
        id: 'ado-99881',
        title: 'Q1 roadmap draft',
        status: 'In Review',
        ticketUrl: '(placeholder)',
        prUrls: ['(placeholder)'],
        commitsUrl: '(placeholder)',
        assignedTo: 'Dana',
        projectId: SeedIds.projectCorePlatform,
      },
    ],
  },

  [SeedIds.teamMemberEvan]: {
    activitySnapshot: {
      bullets: ['Repeated friction: CI flakiness on integration tests is blocking progress.'],
      lastUpdatedIso: isoNowMinusDays(2),
      quickTags: ['CI', 'FlakyTests'],
    },
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
}

export type TeamNoteLocalExtras = Pick<TeamMember['notes'][number], 'adoWorkItemId' | 'prUrl'>

export const teamNoteLocalExtrasById: Record<string, TeamNoteLocalExtras> = {
  [SeedIds.noteAlice6]: { adoWorkItemId: '12345' },
  [SeedIds.noteAlice4]: { adoWorkItemId: '12345' },
  [SeedIds.noteAlice2]: { prUrl: '(placeholder)' },
  [SeedIds.noteAlice1]: { adoWorkItemId: '12345' },
  [SeedIds.noteBob1]: { prUrl: '(placeholder)' },
}

