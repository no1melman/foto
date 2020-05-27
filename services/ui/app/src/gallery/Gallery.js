import React, { useEffect, useState } from 'react';

import './gallery.scss';

const Gallery = ({ refresh }) => {
  const [photos, setPhotos] = useState([]);
  const [photoBlobs, setPhotoBlogs] = useState({});

  const getPhotos = () =>
    fetch('/api/photos')
      .then(res => res.json())
      .then(res => res.result)
      .then(setPhotos);

  useEffect(() => {
    getPhotos();
  }, [refresh]);

  useEffect(() => {
    if (photos.length === 0) return;

    Promise.all(
      photos.map(p =>
        fetch(`/api/photos/${p.id}/download`)
          .then(res => res.blob())
          .then(blob => ({
            blob,
            id: p.id,
          }))
      )
    ).then(imageBlobs => {
      setPhotoBlogs(
        photos.reduce(
          (prev, p) => ({
            ...prev,
            [p.id]: (imageBlobs.find(b => b.id === p.id) || { blob: undefined })
              .blob,
          }),
          {}
        )
      );
    });
  }, [photos]);

  const createUrl = blob =>
    (window.URL || window.webkitURL).createObjectURL(blob);

  const deleteImage = id => e => {
    e.preventDefault();
    e.stopPropagation();

    fetch(`/api/photos/${id}`, {
      method: 'DELETE',
    }).then(() => getPhotos());
  };

  return (
    <div className="gallery">
      {photos.map(p => (
        <div key={p.id} className="gallery-item">
          <p>{p.fileName}</p>
          {photoBlobs[p.id] && (
            <img src={createUrl(photoBlobs[p.id], p.fileName)} width="100%" />
          )}
          <button onClick={deleteImage(p.id)}>Delete</button>
        </div>
      ))}
    </div>
  );
};

export default Gallery;
