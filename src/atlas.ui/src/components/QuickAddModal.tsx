import { useState } from 'react'
import type { KeyboardEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { createRisk } from '../app/api/risks'
import { createTask } from '../app/api/tasks'
import { addTeamNote } from '../app/api/teamMembers'
import { useAppCache } from '../app/queries/useAppCache'
import { useTeam } from '../app/queries/hooks'
import { useSelectionActions } from '../app/state/SelectionState'
import type { NoteTag } from '../app/types'
import { reportSaveError } from '../app/utils'
import { Modal } from './Modal'

type QuickAddKind = 'task' | 'risk' | 'note'

const NOTE_TAGS: NoteTag[] = ['Quick', 'Standup', 'Progress', 'Praise', 'Concern', 'Blocker']

export function QuickAddModal({ isOpen, onClose }: { isOpen: boolean; onClose: () => void }) {
  const team = useTeam()
  const cache = useAppCache()
  const navigate = useNavigate()
  const { selectTeamMember } = useSelectionActions()

  const [kind, setKind] = useState<QuickAddKind>('task')
  const [saving, setSaving] = useState(false)

  const [taskTitle, setTaskTitle] = useState('')
  const [riskTitle, setRiskTitle] = useState('')

  const [memberId, setMemberId] = useState('')
  const [noteTag, setNoteTag] = useState<NoteTag>('Quick')
  const [noteTitle, setNoteTitle] = useState('')
  const [noteText, setNoteText] = useState('')
  const [noteAdo, setNoteAdo] = useState('')
  const [notePr, setNotePr] = useState('')

  function resetForm() {
    setKind('task')
    setTaskTitle('')
    setRiskTitle('')
    setMemberId('')
    setNoteTag('Quick')
    setNoteTitle('')
    setNoteText('')
    setNoteAdo('')
    setNotePr('')
    setSaving(false)
  }

  function handleClose() {
    if (saving) return
    resetForm()
    onClose()
  }

  async function handleCreate() {
    if (saving) return
    setSaving(true)
    try {
      if (kind === 'task') {
        const title = taskTitle.trim() || 'New task'
        const id = await createTask({
          title,
          priority: 'Medium',
          status: 'NotStarted',
          estimatedDurationText: '1h',
          estimateConfidence: 'Medium',
          notes: '',
          dependencyTaskIds: [],
        })
        cache.addTask({
          id,
          title,
          priority: 'Medium',
          status: 'Not Started',
          estimatedDurationText: '1h',
          estimateConfidence: 'Medium',
          notes: '',
          dependencyTaskIds: [],
          lastTouchedIso: new Date().toISOString(),
        })
        resetForm()
        onClose()
        navigate(`/tasks/${id}`)
        return
      }

      if (kind === 'risk') {
        const title = riskTitle.trim() || 'New risk'
        const id = await createRisk({
          title,
          status: 'Open',
          severity: 'Medium',
          description: '',
          evidence: '',
        })
        cache.addRisk({
          id,
          title,
          status: 'Open',
          severity: 'Medium',
          description: '',
          evidence: '',
          linkedTaskIds: [],
          linkedTeamMemberIds: [],
          history: [],
          lastUpdatedIso: new Date().toISOString(),
        })
        resetForm()
        onClose()
        navigate(`/risks/${id}`)
        return
      }

      const text = noteText.trim()
      if (!memberId || !text) {
        window.alert('Select a team member and enter note text before creating.')
        return
      }
      const member = team.find((m) => m.id === memberId)
      if (!member) {
        window.alert('That team member is no longer available. Refresh and try again.')
        return
      }

      const title = noteTitle.trim()
      const ado = noteAdo.trim()
      const pr = notePr.trim()
      const id = await addTeamNote(memberId, {
        tag: noteTag,
        title: title || undefined,
        text,
        adoWorkItemId: ado || undefined,
        prUrl: pr || undefined,
      })
      const now = new Date().toISOString()
      cache.updateTeamMember({
        ...member,
        notes: [
          {
            id,
            createdIso: now,
            lastModifiedIso: now,
            tag: noteTag,
            title: title || undefined,
            text,
            adoWorkItemId: ado || undefined,
            prUrl: pr || undefined,
          },
          ...member.notes,
        ],
      })
      selectTeamMember(memberId)
      resetForm()
      onClose()
      navigate(`/team/${memberId}/notes`)
    } catch (err) {
      reportSaveError(err, `Unable to create ${kind === 'note' ? 'note' : kind} right now. Please try again.`)
    } finally {
      setSaving(false)
    }
  }

  const canCreate =
    kind === 'task' || kind === 'risk' || (kind === 'note' && Boolean(memberId) && Boolean(noteText.trim()))

  function onTitleKeyDown(e: KeyboardEvent<HTMLInputElement>) {
    if (e.key === 'Enter' && canCreate && !saving) {
      e.preventDefault()
      void handleCreate()
    }
  }

  return (
    <Modal
      title="Quick Add"
      isOpen={isOpen}
      onClose={handleClose}
      footer={
        <div className="row" style={{ marginTop: 0 }}>
          <button className="btn btnSecondary" disabled={!canCreate || saving} onClick={() => void handleCreate()}>
            {saving ? 'Creating…' : 'Create'}
          </button>
          <button className="btn btnGhost" disabled={saving} onClick={handleClose}>
            Cancel
          </button>
        </div>
      }
    >
      <div className="chipRow" style={{ marginBottom: 14 }}>
        {(
          [
            { id: 'task', label: 'Task' },
            { id: 'risk', label: 'Risk' },
            { id: 'note', label: 'Team note' },
          ] as const
        ).map((opt) => (
          <button
            key={opt.id}
            type="button"
            className={`chipBtn ${kind === opt.id ? 'chipBtnActive' : ''}`}
            onClick={() => setKind(opt.id)}
            disabled={saving}
          >
            {opt.label}
          </button>
        ))}
      </div>

      {kind === 'task' ? (
        <div className="fieldGrid">
          <label className="field">
            <div className="fieldLabel">Title</div>
            <input
              className="input"
              value={taskTitle}
              onChange={(e) => setTaskTitle(e.target.value)}
              onKeyDown={onTitleKeyDown}
              placeholder="New task"
              autoFocus
            />
          </label>
          <div className="mutedSmall">Creates with Medium priority and Not Started status. Edit details on the task page.</div>
        </div>
      ) : null}

      {kind === 'risk' ? (
        <div className="fieldGrid">
          <label className="field">
            <div className="fieldLabel">Title</div>
            <input
              className="input"
              value={riskTitle}
              onChange={(e) => setRiskTitle(e.target.value)}
              onKeyDown={onTitleKeyDown}
              placeholder="New risk"
              autoFocus
            />
          </label>
          <div className="mutedSmall">Creates as Open / Medium severity. Edit details on the risk page.</div>
        </div>
      ) : null}

      {kind === 'note' ? (
        <div className="fieldGrid">
          <label className="field">
            <div className="fieldLabel">Team member</div>
            <select className="select" value={memberId} onChange={(e) => setMemberId(e.target.value)} autoFocus>
              <option value="">Select a member…</option>
              {team.map((m) => (
                <option key={m.id} value={m.id}>
                  {m.name}
                </option>
              ))}
            </select>
          </label>
          {team.length === 0 ? (
            <div className="mutedSmall">No team members yet. Import from Azure DevOps in Settings.</div>
          ) : null}
          <label className="field">
            <div className="fieldLabel">Tag</div>
            <select className="select" value={noteTag} onChange={(e) => setNoteTag(e.target.value as NoteTag)}>
              {NOTE_TAGS.map((t) => (
                <option key={t} value={t}>
                  {t}
                </option>
              ))}
            </select>
          </label>
          <label className="field">
            <div className="fieldLabel">Title (optional)</div>
            <input
              className="input"
              value={noteTitle}
              onChange={(e) => setNoteTitle(e.target.value)}
              placeholder="e.g., 1:1 follow-ups, Standup recap…"
            />
          </label>
          <label className="field">
            <div className="fieldLabel">ADO work item id (optional)</div>
            <input
              className="input"
              value={noteAdo}
              onChange={(e) => setNoteAdo(e.target.value)}
              placeholder="e.g., 12345"
            />
          </label>
          <label className="field">
            <div className="fieldLabel">PR URL (optional)</div>
            <input
              className="input"
              value={notePr}
              onChange={(e) => setNotePr(e.target.value)}
              placeholder="https://…"
            />
          </label>
          <label className="field">
            <div className="fieldLabel">Note</div>
            <textarea
              className="textarea"
              value={noteText}
              onChange={(e) => setNoteText(e.target.value)}
              placeholder="Write your note…"
            />
          </label>
        </div>
      ) : null}
    </Modal>
  )
}
