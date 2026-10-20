// Shows an error message from the API. Renders nothing when there is no error.
export default function ErrorBanner({ message }) {
  if (!message) {
    return null;
  }

  return (
    <p className="error-banner" role="alert">
      {message}
    </p>
  );
}
