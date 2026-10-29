import ErrorBanner from "../components/ErrorBanner.jsx";
import IdeaComposer from "../components/IdeaComposer.jsx";
import IdeaFilters from "../components/IdeaFilters.jsx";
import IdeaList from "../components/IdeaList.jsx";
import { useIdeas } from "../hooks/useIdeas.js";

// Shows the ideas screen. All the logic lives in useIdeas (constitution, principle 3).
export default function IdeasPage() {
  const ideas = useIdeas();

  return (
    <section className="ideas-page">
      <h1>Mis ideas</h1>

      <IdeaComposer mode="create" allTags={ideas.allTags} onSubmit={ideas.create} />

      <ErrorBanner message={ideas.error} />

      <IdeaFilters
        archived={ideas.archived}
        onArchivedChange={ideas.setArchived}
        search={ideas.search}
        onSearchChange={ideas.setSearch}
        tag={ideas.tag}
        tagOptions={ideas.tagOptions}
        onTagChange={ideas.setTag}
      />

      <IdeaList
        items={ideas.items}
        archived={ideas.archived}
        filtersActive={ideas.filtersActive}
        allTags={ideas.allTags}
        opened={ideas.opened}
        editing={ideas.editing}
        conflict={ideas.conflict}
        formKey={ideas.formKey}
        onOpen={ideas.openIdea}
        onCloseOpened={ideas.closeOpened}
        onEdit={ideas.startEdit}
        onSaveEdit={ideas.saveEdit}
        onCancelEdit={ideas.cancelEdit}
        onUseLatest={ideas.loadLatestVersion}
        onToggleArchive={ideas.toggleArchive}
        onDelete={ideas.remove}
        onClearFilters={ideas.clearFilters}
      />
    </section>
  );
}
