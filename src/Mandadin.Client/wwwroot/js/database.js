
const notes = new PouchDB("notas");

function mapDocument(document) {
  console.log(document);
  return {
    Id: document._id,
    Content: document.Content,
    Hide: document.Hide,
  }
}

function mapAllDocs({ total_rows, offset, rows }) {
  console.log({ total_rows, offset });
  console.table(rows);
  return rows.map(row => {
    return {
      Id: row.id,
      Content: row.doc.Content,
      Hide: row.doc.Hide,
    }
  });
}


export const FindNotes = async () => {
  let result = await notes.allDocs({ include_docs: true }).then(mapAllDocs);
  console.log(result);
  return { Notes: result };
}

export const SaveNote = async (Content) => {
  const note = { Content, _id: `${Date.now()}`, Hide: false }
  const { id, ok, rev } = await notes.put(note);
  console.log({ id, ok, rev });
  return { Id: id, Ok: ok, Rev: rev };
}

export const FindNote = async (noteid) => {
  let result = await notes.get(noteid).then(mapDocument)
  return result;
}