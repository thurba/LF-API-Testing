{{ range .Versions }}
<a name="{{ .Tag.Name }}"></a>
## {{ if .Tag.Previous }}[{{ .Tag.Name }}]{{ else }}{{ .Tag.Name }}{{ end }} - {{ datetime "2006-01-02" .Tag.Date }}
{{ range .CommitGroups -}}
### {{ .Title }}
{{ range .Commits -}}
- {{ if .Scope }}**{{ .Scope }}:** {{ end }}{{ .Subject }}
  {{ end }}
  {{ end -}}

### Commits
{{ range .Commits -}}
{{- if not .Merge -}}
- {{ if .Scope }}**{{ .Scope }}:** {{ end }}{{ datetime "2006-01-02" .Committer.Date }}: [[`{{ .Hash.Short }}`]]({{ $.Info.RepositoryURL }}/commits/{{.Hash.Long}}) - {{ .Header }} - {{ .Committer.Name }}
  {{ end -}}
  {{ end }}

{{- if .RevertCommits -}}
### Reverts
{{ range .RevertCommits -}}
- {{ datetime "2006-01-02" .Committer.Date }}: [[`{{ .Hash.Short }}`]]({{ $.Info.RepositoryURL }}/commits/{{.Hash.Long}}) - {{ .Revert.Header }}
  {{ end }}
  {{ end -}}

{{- if .MergeCommits -}}
### Pull Requests
{{ range .MergeCommits -}}
- {{ datetime "2006-01-02" .Committer.Date }}: [[`{{ .Hash.Short }}`]]({{ $.Info.RepositoryURL }}/commits/{{.Hash.Long}}) - {{ replace .Header "/issues/" "/pull-requests/" 1 }}
  {{ end }}
  {{ end -}}

{{- if .NoteGroups -}}
{{ range .NoteGroups -}}
### {{ .Title }}
{{ range .Notes }}
{{ .Body }}
{{ end }}
{{ end -}}
{{ end -}}
{{ end -}}

{{- if .Versions }}
{{ range .Versions -}}
{{ if .Tag.Previous -}}
[{{ .Tag.Name }}]: {{ $.Info.RepositoryURL }}/compare/{{ if (eq .Tag.Name "Unreleased") }}master{{ else }}{{ .Tag.Name }}{{ end }}%0D{{ .Tag.Previous.Name }}
{{ end -}}
{{ end -}}
{{ end -}}
